using System.Collections.Generic;
using SideBet.Core;
using Xunit;

namespace SideBet.Core.Tests
{
    public class DiceDuelTests
    {
        /// <summary>A scripted "RNG" that returns a fixed sequence of rolls, for determinism.</summary>
        private static System.Func<int> Sequence(params int[] rolls)
        {
            var queue = new Queue<int>(rolls);
            return () => queue.Dequeue();
        }

        [Fact]
        public void Higher_roll_wins()
        {
            // Alice rolls 5, Bob rolls 2 -> Alice (outcome "A").
            var duel = new DiceDuel("A", "Alice", "B", "Bob", Sequence(5, 2), Odds.FromDecimal(2m));
            Assert.Equal("A", duel.Resolve());
        }

        [Fact]
        public void Ties_are_rerolled()
        {
            // (3,3) tie -> reroll -> (1,6) -> Bob (outcome "B").
            var duel = new DiceDuel("A", "Alice", "B", "Bob", Sequence(3, 3, 1, 6), Odds.FromDecimal(2m));
            Assert.Equal("B", duel.Resolve());
        }

        [Fact]
        public void Exposes_one_outcome_per_competitor()
        {
            var duel = new DiceDuel("A", "Alice", "B", "Bob", Sequence(1, 2), Odds.FromDecimal(2m));
            Assert.Equal(2, duel.Outcomes.Count);
        }
    }
}
