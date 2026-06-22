using System;

namespace SideBet.Core
{
    /// <summary>What kind of chaos a swing unleashes. The brain emits these; a mini-game interprets them.</summary>
    public enum ModifierKind
    {
        MomentumNudge,        // Tier 1 — a gentle shove
        OpponentChaos,        // Tier 2 — variance / "the opponent gets rattled and wild"
        CrowdInterference,    // Tier 3 — the no-physics twin of "fans storm the field"
    }

    /// <summary>
    /// An immutable instruction from the betting brain to a mini-game. DATA ONLY — it says WHAT
    /// happened (a tier was crossed) and the narrative target, never how to apply it. The mini-game
    /// decides the effect (and must obey the chaos-not-payout law: effects add variance/rubber-band,
    /// they never reward the bet that triggered them).
    /// </summary>
    public readonly struct Modifier
    {
        public long Sequence { get; }                  // server-authoritative ordering
        public int Tier { get; }                       // which swing tier fired it (1..N)
        public ModifierKind Kind { get; }
        public string TriggeredAgainstTeamId { get; }  // the team the market swung AGAINST (for narration)
        public double Magnitude { get; }               // 0..1, scales the effect
        public string Reason { get; }
        public double AtSeconds { get; }

        public Modifier(long sequence, int tier, ModifierKind kind,
                        string triggeredAgainstTeamId, double magnitude, string reason, double atSeconds)
        {
            Sequence = sequence;
            Tier = tier;
            Kind = kind;
            TriggeredAgainstTeamId = triggeredAgainstTeamId;
            Magnitude = magnitude;
            Reason = reason;
            AtSeconds = atSeconds;
        }
    }

    /// <summary>The live line for one team: where it started, where the money has moved it, and the swing.</summary>
    public readonly struct TeamLine
    {
        public string TeamId { get; }
        public double BaselineWinProb { get; }
        public double LiveWinProb { get; }
        public double Pressure { get; }              // -1..1 (signed money pressure)
        public double Swing => LiveWinProb - BaselineWinProb;

        public TeamLine(string teamId, double baselineWinProb, double liveWinProb, double pressure)
        {
            TeamId = teamId;
            BaselineWinProb = baselineWinProb;
            LiveWinProb = liveWinProb;
            Pressure = pressure;
        }
    }

    /// <summary>The whole two-team line at a moment in time. v1 is strictly 2 teams.</summary>
    public sealed class LineState
    {
        public TeamLine A { get; }
        public TeamLine B { get; }
        public double MaxSwing { get; }

        public LineState(TeamLine a, TeamLine b)
        {
            A = a;
            B = b;
            MaxSwing = Math.Max(Math.Abs(a.Swing), Math.Abs(b.Swing));
        }
    }

    /// <summary>
    /// Every design dial lives here as data, so balancing is config tweaks, not code edits.
    /// (These defaults are a starting point — tune them in the sim.)
    /// </summary>
    public sealed class PressureConfig
    {
        public double PressureScale = 500.0;             // ~chips that produce tanh≈0.83 of pressure
        public double DecayHalfLifeSeconds = 20.0;       // recent money matters more (line moves AND settles)
        public double LineSensitivity = 0.35;            // max win-prob shift from full pressure
        public double[] TierThresholds = { 0.08, 0.18, 0.28 }; // MaxSwing needed to enter tier 1/2/3
        public double HysteresisDrop = 0.04;             // must fall this far below entry to re-arm a tier
        public double TierCooldownSeconds = 8.0;         // anti-strobe: min seconds between fires
        public double FinalLockoutSeconds = 10.0;        // no live bets in the last N seconds
    }
}
