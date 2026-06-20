using System;

namespace SideBet.Core
{
    /// <summary>
    /// Immutable betting odds. Decimal odds are canonical: the total return per unit staked,
    /// stake included. A 1-chip wager at 2.50 returns 2.50 (1.50 profit). Includes
    /// American-odds conversion because that's the mental model we actually think in.
    /// Pure C# — no UnityEngine — so it is unit-testable off the engine.
    /// </summary>
    public readonly struct Odds
    {
        /// <summary>Decimal odds, e.g. 2.50. Always &gt; 1.0.</summary>
        public decimal Decimal { get; }

        private Odds(decimal dec)
        {
            if (dec <= 1m)
                throw new ArgumentOutOfRangeException(nameof(dec), dec, "Decimal odds must be > 1.0");
            Decimal = dec;
        }

        public static Odds FromDecimal(decimal dec) => new Odds(dec);

        /// <summary>+150 =&gt; 2.50, -200 =&gt; 1.50. American odds of 0 are invalid.</summary>
        public static Odds FromAmerican(int american)
        {
            if (american == 0)
                throw new ArgumentOutOfRangeException(nameof(american), "American odds cannot be 0");
            decimal dec = american > 0
                ? 1m + american / 100m
                : 1m + 100m / -american;
            return new Odds(dec);
        }

        /// <summary>American representation (&gt;= +100 or &lt;= -100).</summary>
        public int American =>
            Decimal >= 2m
                ? (int)Math.Round((Decimal - 1m) * 100m)
                : (int)Math.Round(-100m / (Decimal - 1m));

        /// <summary>Bookmaker-implied win probability (ignores vig): 1 / decimal.</summary>
        public decimal ImpliedProbability => 1m / Decimal;

        public override string ToString() =>
            $"{Decimal:0.00} ({(American > 0 ? "+" : "")}{American})";
    }
}
