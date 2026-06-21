using System;
using System.Collections.Generic;
using System.Linq;

namespace SideBet.Core
{
    /// <summary>
    /// Orchestrates a full match: a fixed roster of players whose bankrolls PERSIST across a
    /// sequence of rounds, plus the running leaderboard. One GameSession lives for the whole
    /// play night; each round is a fresh <see cref="Round"/> over the SAME Player objects, so
    /// chips won and lost carry forward.
    ///
    /// Pure C# (no engine). The Unity layer (MatchNetwork) just drives this on the server and
    /// mirrors its state to clients — so the entire game loop is unit-testable off the engine.
    /// </summary>
    public sealed class GameSession
    {
        private readonly Dictionary<string, Player> _players;
        private IMiniGame _currentGame;

        public int RoundNumber { get; private set; }
        public Round CurrentRound { get; private set; }
        public IReadOnlyCollection<Player> Players => _players.Values;

        public GameSession(IEnumerable<Player> players)
        {
            _players = players.ToDictionary(p => p.Id);
            if (_players.Count < 2)
                throw new ArgumentException("A session needs at least two players.", nameof(players));
        }

        public bool RoundInProgress =>
            CurrentRound != null && CurrentRound.Phase != RoundPhase.Settled;

        /// <summary>Begin a new round built around <paramref name="game"/> and open betting.</summary>
        public Round StartRound(IMiniGame game)
        {
            if (game == null) throw new ArgumentNullException(nameof(game));
            if (RoundInProgress)
                throw new InvalidOperationException("Finish the current round before starting another.");

            _currentGame = game;
            CurrentRound = new Round(_players.Values);
            CurrentRound.OpenBetting(game.Outcomes);
            RoundNumber++;
            return CurrentRound;
        }

        public Wager PlaceBet(string playerId, string outcomeId, long stake)
        {
            RequireRound();
            return CurrentRound.PlaceBet(playerId, outcomeId, stake);
        }

        /// <summary>Lock betting, play the mini-game out, settle, and return the round's results.</summary>
        public IReadOnlyList<SettlementEntry> PlayAndSettle()
        {
            RequireRound();
            CurrentRound.LockBetting();
            CurrentRound.StartPlay();
            CurrentRound.ResolveWith(_currentGame.Resolve());
            return CurrentRound.Settle();
        }

        /// <summary>Players ranked by chips, richest first.</summary>
        public IReadOnlyList<Player> Standings() =>
            _players.Values.OrderByDescending(p => p.Bankroll).ToList();

        public Player PlayerById(string id) => _players[id];

        private void RequireRound()
        {
            if (CurrentRound == null)
                throw new InvalidOperationException("No round has been started.");
        }
    }
}
