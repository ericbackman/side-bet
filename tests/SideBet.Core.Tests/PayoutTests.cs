using System;
using SideBet.Core;
using Xunit;

namespace SideBet.Core.Tests
{
    public class PayoutTests
    {
        [Fact]
        public void Even_money_doubles_the_stake()
        {
            var odds = Odds.FromDecimal(2.0m);
            Assert.Equal(200, Payouts.WinningReturn(100, odds));
            Assert.Equal(100, Payouts.Profit(100, odds));
        }

        [Fact]
        public void Favourite_pays_less_than_even()
        {
            var odds = Odds.FromAmerican(-200); // decimal 1.50
            Assert.Equal(150, Payouts.WinningReturn(100, odds));
            Assert.Equal(50, Payouts.Profit(100, odds));
        }

        [Fact]
        public void Returns_are_floored_to_whole_chips()
        {
            // 3 * 1.33 = 3.99 -> floored to 3, so profit is 0 (house-safe rounding).
            var odds = Odds.FromDecimal(1.33m);
            Assert.Equal(3, Payouts.WinningReturn(3, odds));
            Assert.Equal(0, Payouts.Profit(3, odds));
        }

        [Fact]
        public void Non_positive_stake_throws()
        {
            var odds = Odds.FromDecimal(2.0m);
            Assert.Throws<ArgumentOutOfRangeException>(() => Payouts.WinningReturn(0, odds));
            Assert.Throws<ArgumentOutOfRangeException>(() => Payouts.WinningReturn(-5, odds));
        }
    }
}
