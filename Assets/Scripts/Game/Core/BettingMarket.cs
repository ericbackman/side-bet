using System;
using System.Collections.Generic;
using System.Linq;

namespace SideBet.Core
{
    /// <summary>
    /// The betting market for one round: a set of outcomes, the wagers placed on them, and
    /// settlement. Authoritative on the server — it debits stakes when bets are placed and
    /// credits winners on settle. Odds are locked at placement, so later changes don't
    /// affect tickets already written.
    /// </summary>
    public sealed class BettingMarket
    {
        private readonly Dictionary<string, Outcome> _outcomes;
        private readonly List<Wager> _wagers = new List<Wager>();

        public bool IsOpen { get; private set; } = true;
        public bool IsSettled { get; private set; }
        public IReadOnlyList<Wager> Wagers => _wagers;
        public IReadOnlyCollection<Outcome> Outcomes => _outcomes.Values;

        public BettingMarket(IEnumerable<Outcome> outcomes)
        {
            _outcomes = outcomes.ToDictionary(o => o.Id);
            if (_outcomes.Count < 2)
                throw new ArgumentException("A market needs at least two outcomes.", nameof(outcomes));
        }

        public Outcome OutcomeById(string id) => _outcomes.TryGetValue(id, out var o) ? o : null;

        /// <summary>Place a bet: validates market/outcome/funds, then debits the stake.</summary>
        public Wager PlaceBet(Player player, string outcomeId, long stake)
        {
            if (!IsOpen) throw new InvalidOperationException("Betting is closed.");
            if (!_outcomes.TryGetValue(outcomeId, out var outcome))
                throw new ArgumentException($"Unknown outcome '{outcomeId}'.", nameof(outcomeId));
            if (!player.CanAfford(stake))
                throw new InvalidOperationException(
                    $"{player.Name} cannot stake {stake} (bankroll {player.Bankroll}).");

            player.Debit(stake);
            var wager = new Wager(player.Id, outcomeId, stake, outcome.Odds);
            _wagers.Add(wager);
            return wager;
        }

        /// <summary>Close betting (lock the market) before the mini-game resolves.</summary>
        public void Lock() => IsOpen = false;

        /// <summary>
        /// Settle against the winning outcome, crediting winners. Returns each player's net
        /// chips for the round (positive = profit, negative = loss) for display. Settling
        /// twice is an error.
        /// </summary>
        public IReadOnlyList<SettlementEntry> Settle(
            string winningOutcomeId, IReadOnlyDictionary<string, Player> playersById)
        {
            if (IsSettled) throw new InvalidOperationException("Market already settled.");
            if (!_outcomes.ContainsKey(winningOutcomeId))
                throw new ArgumentException($"Unknown winning outcome '{winningOutcomeId}'.", nameof(winningOutcomeId));
            IsOpen = false;

            var net = new Dictionary<string, long>();
            foreach (var w in _wagers)
            {
                long delta = -w.Stake; // stake was already debited when the bet was placed
                if (w.OutcomeId == winningOutcomeId)
                {
                    long ret = Payouts.WinningReturn(w.Stake, w.OddsAtPlacement);
                    playersById[w.PlayerId].Credit(ret);
                    delta += ret;
                }
                net[w.PlayerId] = (net.TryGetValue(w.PlayerId, out var cur) ? cur : 0) + delta;
            }

            IsSettled = true;
            return net.Select(kv => new SettlementEntry(kv.Key, kv.Value)).ToList();
        }
    }

    /// <summary>A player's net chip result for a settled round.</summary>
    public sealed class SettlementEntry
    {
        public string PlayerId { get; }
        public long Net { get; }

        public SettlementEntry(string playerId, long net)
        {
            PlayerId = playerId;
            Net = net;
        }
    }
}
