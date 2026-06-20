using System;
using SideBet.Core;
using Xunit;

namespace SideBet.Core.Tests
{
    public class OddsTests
    {
        [Theory]
        [InlineData(150, 2.50)]
        [InlineData(-200, 1.50)]
        [InlineData(100, 2.00)]
        [InlineData(-110, 1.9090909090909090909090909091)]
        public void FromAmerican_converts_to_decimal(int american, decimal expectedDecimal)
        {
            Assert.Equal(expectedDecimal, Odds.FromAmerican(american).Decimal, 10);
        }

        [Theory]
        [InlineData(2.00, 100)]
        [InlineData(2.50, 150)]
        [InlineData(1.50, -200)]
        public void Decimal_converts_back_to_american(decimal dec, int expectedAmerican)
        {
            Assert.Equal(expectedAmerican, Odds.FromDecimal(dec).American);
        }

        [Fact]
        public void ImpliedProbability_is_inverse_of_decimal()
        {
            Assert.Equal(0.5m, Odds.FromDecimal(2m).ImpliedProbability);
            Assert.Equal(0.25m, Odds.FromDecimal(4m).ImpliedProbability);
        }

        [Fact]
        public void Invalid_odds_throw()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => Odds.FromDecimal(1.0m));
            Assert.Throws<ArgumentOutOfRangeException>(() => Odds.FromDecimal(0.5m));
            Assert.Throws<ArgumentOutOfRangeException>(() => Odds.FromAmerican(0));
        }
    }
}
