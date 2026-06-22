using System;

namespace SideBet.Core
{
    /// <summary>A snapshot of a live match the server mirrors to clients (small, value-type-friendly).</summary>
    public readonly struct MatchSnapshot
    {
        public int ScoreA { get; }
        public int ScoreB { get; }
        public double Momentum { get; }          // -1 (toward B) .. +1 (toward A)
        public double SecondsRemaining { get; }
        public string LastEvent { get; }

        public MatchSnapshot(int scoreA, int scoreB, double momentum, double secondsRemaining, string lastEvent)
        {
            ScoreA = scoreA;
            ScoreB = scoreB;
            Momentum = momentum;
            SecondsRemaining = secondsRemaining;
            LastEvent = lastEvent;
        }
    }

    /// <summary>
    /// A mini-game that plays out over time and can consume <see cref="Modifier"/> events. Extends
    /// <see cref="IMiniGame"/>, so the server still calls Resolve() for the winner and non-ticking
    /// games (DiceDuel) are unaffected. THIS is the seam that lets the same betting brain drive a
    /// cheap no-physics match now and a physics soccer match later — only the implementation changes.
    /// </summary>
    public interface ITickingMiniGame : IMiniGame
    {
        void Tick(double dtSeconds);            // fixed-step; deterministic with an injected RNG
        string ApplyModifier(in Modifier m);    // the ONLY game-specific seam; returns an event label
        bool IsResolved { get; }
        MatchSnapshot Snapshot { get; }
    }
}
