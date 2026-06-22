namespace SideBet.Core
{
    /// <summary>
    /// Deterministic randomness so sims/tests are reproducible and the SERVER owns the RNG
    /// (a client can never influence a roll). Inject this everywhere instead of System.Random.
    /// </summary>
    public interface IRandomSource
    {
        /// <summary>A value in [0, 1).</summary>
        double NextDouble();
    }

    /// <summary>Seedable PRNG (mulberry32). Same seed → same sequence.</summary>
    public sealed class SeededRandom : IRandomSource
    {
        private uint _a;

        public SeededRandom(uint seed) { _a = seed; }

        public double NextDouble()
        {
            unchecked
            {
                _a += 0x6D2B79F5u;
                uint t = _a;
                t = (t ^ (t >> 15)) * (t | 1u);
                t ^= t + (t ^ (t >> 7)) * (t | 61u);
                t ^= t >> 14;
                return t / 4294967296.0;
            }
        }
    }
}
