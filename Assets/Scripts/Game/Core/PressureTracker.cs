using System;
using System.Collections.Generic;

namespace SideBet.Core
{
    /// <summary>
    /// PURE projection: the live wagers + the clock → a two-team line. It keeps NO internal
    /// ledger — the wager list (owned by <see cref="LiveBettingMarket"/>) is the single source of
    /// truth, so the line can never drift from what actually pays out. Deterministic and engine-free.
    ///
    /// Model: each wager is decayed by a half-life (recent money matters more, so the line moves
    /// AND settles back — that's the fun), netted between the two teams, squashed to a bounded
    /// pressure via tanh, and used to nudge the baseline win-prob. Swing = displacement from the
    /// opening line.
    /// </summary>
    public sealed class PressureTracker
    {
        private readonly string _teamA;
        private readonly string _teamB;
        private readonly double _baselineA;
        private readonly PressureConfig _cfg;

        public PressureTracker(string teamA, string teamB, double baselineWinProbA, PressureConfig cfg)
        {
            _teamA = teamA;
            _teamB = teamB;
            _baselineA = baselineWinProbA;
            _cfg = cfg;
        }

        public LineState LineAt(IReadOnlyList<LiveWager> wagers, double nowSeconds)
        {
            double weightedA = 0, weightedB = 0;
            for (int i = 0; i < wagers.Count; i++)
            {
                var w = wagers[i];
                double age = Math.Max(0.0, nowSeconds - w.PlacedAtSeconds);
                double decay = Math.Pow(0.5, age / _cfg.DecayHalfLifeSeconds);
                double amt = w.Stake * decay;
                if (w.TeamId == _teamA) weightedA += amt;
                else if (w.TeamId == _teamB) weightedB += amt;
                else throw new ArgumentException($"Wager on unknown team '{w.TeamId}'.");
            }

            double pressureA = Math.Tanh((weightedA - weightedB) / _cfg.PressureScale);
            double liveA = Clamp(_baselineA + _cfg.LineSensitivity * pressureA, 0.05, 0.95);
            double liveB = 1.0 - liveA;

            var a = new TeamLine(_teamA, _baselineA, liveA, pressureA);
            var b = new TeamLine(_teamB, 1.0 - _baselineA, liveB, -pressureA);
            return new LineState(a, b);
        }

        private static double Clamp(double v, double lo, double hi) => v < lo ? lo : (v > hi ? hi : v);
    }
}
