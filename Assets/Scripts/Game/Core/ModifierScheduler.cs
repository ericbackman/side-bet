using System.Collections.Generic;

namespace SideBet.Core
{
    /// <summary>
    /// Turns line swings into a stream of <see cref="Modifier"/> events. Stateful but
    /// deterministic given the call sequence. Three guards (all from the adversarial review):
    ///   • EDGE-TRIGGERED — fires once on ENTERING a higher tier, not every tick.
    ///   • HYSTERESIS — won't re-arm a tier until the swing falls back below an exit band
    ///     (stops a line oscillating around a threshold from strobing modifiers on/off).
    ///   • COOLDOWN — an absolute minimum time between fires (stops a whale dumping →
    ///     letting decay drop it → dumping again to machine-gun the top tier).
    /// </summary>
    public sealed class ModifierScheduler
    {
        private readonly PressureConfig _cfg;
        private int _armedTier;                              // tier we've fired for and are holding
        private double _lastFireTime = double.NegativeInfinity;
        private long _sequence;

        public ModifierScheduler(PressureConfig cfg) { _cfg = cfg; }

        public IReadOnlyList<Modifier> Advance(LineState line, double nowSeconds)
        {
            var fired = new List<Modifier>();
            int tier = TierFor(line.MaxSwing);

            // Hysteresis: only step the armed tier DOWN once we've dropped below its exit band.
            if (tier < _armedTier)
            {
                double exit = _cfg.TierThresholds[_armedTier - 1] - _cfg.HysteresisDrop;
                if (line.MaxSwing < exit) _armedTier = tier;
                return fired; // still holding the higher tier — no new fire
            }

            // Entering a higher tier: fire once, if the cooldown has elapsed.
            if (tier > _armedTier && nowSeconds - _lastFireTime >= _cfg.TierCooldownSeconds)
            {
                string against = line.A.Swing < 0 ? line.A.TeamId : line.B.TeamId; // team losing market favor
                double mag = Min01(line.MaxSwing / _cfg.TierThresholds[_cfg.TierThresholds.Length - 1]);
                string reason = $"line swung {line.MaxSwing:P0} against {against}";
                fired.Add(new Modifier(_sequence++, tier, KindForTier(tier), against, mag, reason, nowSeconds));
                _armedTier = tier;
                _lastFireTime = nowSeconds;
            }
            return fired;
        }

        private int TierFor(double swing)
        {
            int t = 0;
            for (int i = 0; i < _cfg.TierThresholds.Length; i++)
                if (swing >= _cfg.TierThresholds[i]) t = i + 1;
            return t;
        }

        private static ModifierKind KindForTier(int tier)
        {
            if (tier <= 1) return ModifierKind.MomentumNudge;
            if (tier == 2) return ModifierKind.OpponentChaos;
            return ModifierKind.CrowdInterference;
        }

        private static double Min01(double v) => v < 0 ? 0 : (v > 1 ? 1 : v);
    }
}
