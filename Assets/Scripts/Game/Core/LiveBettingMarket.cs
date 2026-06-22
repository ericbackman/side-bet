using System;
using System.Collections.Generic;
using System.Linq;

namespace SideBet.Core
{
    /// <summary>A live, in-play bet. Carries its placement TIME (for line decay) and the odds the
    /// SERVER stamped at the moment it processed the bet (clients never set their own odds).</summary>
    public readonly struct LiveWager
    {
        public string PlayerId { get; }
        public string TeamId { get; }
        public long Stake { get; }
        public double PlacedAtSeconds { get; }
        public Odds OddsAtPlacement { get; }

        public LiveWager(string playerId, string teamId, long stake, double placedAtSeconds, Odds oddsAtPlacement)
        {
            PlayerId = playerId;
            TeamId = teamId;
            Stake = stake;
            PlacedAtSeconds = placedAtSeconds;
            OddsAtPlacement = oddsAtPlacement;
        }
    }

    public enum LiveBetReject { None, MatchOver, FinalLockout, UnknownTeam, NonPositiveStake, InsufficientFunds }

    public sealed class LiveBetResult
    {
        public bool Accepted { get; }
        public LiveBetReject Reason { get; }
        public LiveWager Wager { get; }
        public LiveBetResult(bool accepted, LiveBetReject reason, LiveWager wager)
        { Accepted = accepted; Reason = reason; Wager = wager; }
    }

    /// <summary>
    /// In-play betting while the match is live. Bets are COMMITMENTS — no cancels (that's also what
    /// makes selling out your team funny and non-griefable). Server-authoritative: odds are stamped
    /// from the line at receipt, and every rejection returns a typed reason (never a silent no-op).
    ///
    /// Settlement here is fixed-odds-at-placement (reuses <see cref="Payouts"/>). NOTE: with a moving
    /// line that is NOT chip-conservative — fine if chips are score, but a closed economy should use
    /// pari-mutuel. That payout-contract choice is the one decision flagged for Eric in V1-SCOPE.
    /// </summary>
    public sealed class LiveBettingMarket
    {
        private readonly string _teamA;
        private readonly string _teamB;
        private readonly PressureConfig _cfg;
        private readonly List<LiveWager> _wagers = new List<LiveWager>();

        public IReadOnlyList<LiveWager> Wagers => _wagers;
        public bool IsSettled { get; private set; }

        public LiveBettingMarket(string teamA, string teamB, PressureConfig cfg)
        {
            _teamA = teamA;
            _teamB = teamB;
            _cfg = cfg;
        }

        public LiveBetResult PlaceLiveBet(Player player, string teamId, long stake, Odds serverOdds,
                                          double nowSeconds, double matchSecondsRemaining)
        {
            if (matchSecondsRemaining <= 0) return Rejected(LiveBetReject.MatchOver);
            if (matchSecondsRemaining <= _cfg.FinalLockoutSeconds) return Rejected(LiveBetReject.FinalLockout);
            if (teamId != _teamA && teamId != _teamB) return Rejected(LiveBetReject.UnknownTeam);
            if (stake <= 0) return Rejected(LiveBetReject.NonPositiveStake);
            if (!player.CanAfford(stake)) return Rejected(LiveBetReject.InsufficientFunds);

            player.Debit(stake);
            var wager = new LiveWager(player.Id, teamId, stake, nowSeconds, serverOdds);
            _wagers.Add(wager);
            return new LiveBetResult(true, LiveBetReject.None, wager);
        }

        public IReadOnlyList<SettlementEntry> Settle(string winningTeamId, IReadOnlyDictionary<string, Player> players)
        {
            if (IsSettled) throw new InvalidOperationException("Live market already settled.");

            var net = new Dictionary<string, long>();
            foreach (var w in _wagers)
            {
                long delta = -w.Stake; // stake already debited at placement
                if (w.TeamId == winningTeamId)
                {
                    long ret = Payouts.WinningReturn(w.Stake, w.OddsAtPlacement);
                    players[w.PlayerId].Credit(ret);
                    delta += ret;
                }
                net[w.PlayerId] = (net.TryGetValue(w.PlayerId, out var cur) ? cur : 0) + delta;
            }

            IsSettled = true;
            return net.Select(kv => new SettlementEntry(kv.Key, kv.Value)).ToList();
        }

        private static LiveBetResult Rejected(LiveBetReject reason) => new LiveBetResult(false, reason, default);
    }
}
