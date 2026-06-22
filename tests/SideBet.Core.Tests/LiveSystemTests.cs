using System.Collections.Generic;
using SideBet.Core;
using Xunit;

namespace SideBet.Core.Tests
{
    public class LiveSystemTests
    {
        private static PressureConfig Cfg() => new PressureConfig();

        private static LineState LineWithLiveA(double liveA) =>
            new LineState(new TeamLine("A", 0.5, liveA, 0), new TeamLine("B", 0.5, 1 - liveA, 0));

        // ---------- PressureTracker ----------

        [Fact]
        public void Money_moves_the_line_toward_the_backed_team()
        {
            var tr = new PressureTracker("A", "B", 0.5, Cfg());
            var wagers = new List<LiveWager> { new LiveWager("p", "A", 600, 0, Odds.FromDecimal(2)) };
            var line = tr.LineAt(wagers, 0);
            Assert.True(line.A.LiveWinProb > 0.5);
            Assert.True(line.MaxSwing > 0);
        }

        [Fact]
        public void Old_money_decays_so_the_line_settles_back()
        {
            var tr = new PressureTracker("A", "B", 0.5, Cfg());
            var wagers = new List<LiveWager> { new LiveWager("p", "A", 600, 0, Odds.FromDecimal(2)) };
            double swingNow = tr.LineAt(wagers, 0).MaxSwing;
            double swingLater = tr.LineAt(wagers, 40).MaxSwing; // 2 half-lives later
            Assert.True(swingLater < swingNow);
        }

        // ---------- ModifierScheduler ----------

        [Fact]
        public void Crossing_a_tier_fires_once_then_holds_hysteresis()
        {
            var sch = new ModifierScheduler(Cfg());
            var high = LineWithLiveA(0.60); // swing 0.10 -> tier 1
            Assert.Single(sch.Advance(high, 0));      // fires on entry
            Assert.Empty(sch.Advance(high, 1));       // still high -> no re-fire
            Assert.Empty(sch.Advance(high, 2));
        }

        [Fact]
        public void Dropping_below_exit_band_then_rising_refires_after_cooldown()
        {
            var sch = new ModifierScheduler(Cfg());
            sch.Advance(LineWithLiveA(0.60), 0);          // fire tier 1 at t=0
            sch.Advance(LineWithLiveA(0.50), 2);          // swing 0 -> below exit band -> re-arm
            var refire = sch.Advance(LineWithLiveA(0.60), 20); // cooled (>8s) -> fires again
            Assert.Single(refire);
        }

        [Fact]
        public void Cooldown_blocks_immediate_refire()
        {
            var sch = new ModifierScheduler(Cfg());
            sch.Advance(LineWithLiveA(0.60), 0);          // tier 1
            sch.Advance(LineWithLiveA(0.50), 1);          // re-arm down
            var tooSoon = sch.Advance(LineWithLiveA(0.60), 3); // only 3s since last fire (<8) -> blocked
            Assert.Empty(tooSoon);
        }

        // ---------- LiveBettingMarket ----------

        [Fact]
        public void Live_bet_is_rejected_in_the_final_lockout_with_a_typed_reason()
        {
            var market = new LiveBettingMarket("A", "B", Cfg());
            var p = new Player("p", "P", 1000);
            var r = market.PlaceLiveBet(p, "A", 100, Odds.FromDecimal(2), nowSeconds: 50, matchSecondsRemaining: 5);
            Assert.False(r.Accepted);
            Assert.Equal(LiveBetReject.FinalLockout, r.Reason);
            Assert.Equal(1000, p.Bankroll); // not debited
        }

        [Fact]
        public void Accepted_live_bet_debits_and_settles()
        {
            var market = new LiveBettingMarket("A", "B", Cfg());
            var p = new Player("p", "P", 1000);
            var ok = market.PlaceLiveBet(p, "A", 100, Odds.FromDecimal(2), nowSeconds: 10, matchSecondsRemaining: 40);
            Assert.True(ok.Accepted);
            Assert.Equal(900, p.Bankroll);
            market.Settle("A", new Dictionary<string, Player> { ["p"] = p });
            Assert.Equal(1100, p.Bankroll);
        }

        // ---------- MomentumMatch (chaos-not-payout direction + determinism) ----------

        [Fact]
        public void CrowdInterference_rubber_bands_toward_the_trailer_not_the_money()
        {
            var match = new MomentumMatch("A", "B", Odds.FromDecimal(2), Odds.FromDecimal(2), new SeededRandom(7), 60);
            // play until someone leads (or the match ends)
            while (match.Snapshot.ScoreA == match.Snapshot.ScoreB && !match.IsResolved) match.Tick(0.1);

            var snap = match.Snapshot;
            double before = snap.Momentum;
            string against = snap.ScoreA >= snap.ScoreB ? "A" : "B"; // narrative target = the leader's side
            match.ApplyModifier(new Modifier(0, 3, ModifierKind.CrowdInterference, against, 1.0, "test", snap.SecondsRemaining));
            double after = match.Snapshot.Momentum;

            if (snap.ScoreA == snap.ScoreB) Assert.Equal(before, after, 3);     // tie -> no directional push
            else if (snap.ScoreA < snap.ScoreB) Assert.True(after > before);    // A trailing -> momentum toward A
            else Assert.True(after < before);                                   // B trailing -> momentum toward B
        }

        [Fact]
        public void Same_seed_resolves_to_the_same_winner()
        {
            string Run(uint seed)
            {
                var m = new MomentumMatch("A", "B", Odds.FromDecimal(2), Odds.FromDecimal(2), new SeededRandom(seed), 30);
                while (!m.IsResolved) m.Tick(0.1);
                return m.Resolve();
            }
            Assert.Equal(Run(7), Run(7));
        }
    }
}
