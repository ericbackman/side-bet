using System;
using System.Collections.Generic;
using System.Linq;

namespace SideBet.Core
{
    /// <summary>The phases of a single betting round, in order.</summary>
    public enum RoundPhase
    {
        Lobby,
        BettingOpen,
        Locked,
        Playing,
        Resolved,
        Settled
    }

    /// <summary>
    /// Drives one betting round through its phases. The mini-game OUTCOME is supplied from
    /// outside (the server / the actual Unity mini-game) via <see cref="ResolveWith"/> — this
    /// class doesn't know how a match is played, only how chips move around it. That keeps the
    /// betting core engine-free and unit-testable, and lets Stevie's net layer and the
    /// mini-games feed results in. Every transition is guarded so illegal sequences throw
    /// loudly instead of silently corrupting state.
    /// </summary>
    public sealed class Round
    {
        private readonly Dictionary<string, Player> _players;

        public RoundPhase Phase { get; private set; } = RoundPhase.Lobby;
        public BettingMarket Market { get; private set; }
        public string WinningOutcomeId { get; private set; }
        public IReadOnlyList<SettlementEntry> LastSettlement { get; private set; }

        public Round(IEnumerable<Player> players)
        {
            _players = players.ToDictionary(p => p.Id);
            if (_players.Count < 2)
                throw new ArgumentException("Need at least two players.", nameof(players));
        }

        public Player PlayerById(string id) => _players[id];

        public void OpenBetting(IEnumerable<Outcome> outcomes)
        {
            Require(RoundPhase.Lobby);
            Market = new BettingMarket(outcomes);
            Phase = RoundPhase.BettingOpen;
        }

        public Wager PlaceBet(string playerId, string outcomeId, long stake)
        {
            Require(RoundPhase.BettingOpen);
            return Market.PlaceBet(_players[playerId], outcomeId, stake);
        }

        public void LockBetting()
        {
            Require(RoundPhase.BettingOpen);
            Market.Lock();
            Phase = RoundPhase.Locked;
        }

        public void StartPlay()
        {
            Require(RoundPhase.Locked);
            Phase = RoundPhase.Playing;
        }

        /// <summary>Record the server-authoritative mini-game result.</summary>
        public void ResolveWith(string winningOutcomeId)
        {
            Require(RoundPhase.Playing);
            if (Market.OutcomeById(winningOutcomeId) == null)
                throw new ArgumentException($"Unknown outcome '{winningOutcomeId}'.", nameof(winningOutcomeId));
            WinningOutcomeId = winningOutcomeId;
            Phase = RoundPhase.Resolved;
        }

        public IReadOnlyList<SettlementEntry> Settle()
        {
            Require(RoundPhase.Resolved);
            LastSettlement = Market.Settle(WinningOutcomeId, _players);
            Phase = RoundPhase.Settled;
            return LastSettlement;
        }

        private void Require(RoundPhase expected)
        {
            if (Phase != expected)
                throw new InvalidOperationException(
                    $"Action requires phase {expected}, but the round is in {Phase}.");
        }
    }
}
