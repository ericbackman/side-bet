using System;

namespace SideBet.Core
{
    /// <summary>
    /// Payout math for winning wagers. Chips are whole integers, so payouts must round.
    /// </summary>
    public static class Payouts
    {
        /// <summary>
        /// Total chips returned to a WINNING wager (stake included), rounded DOWN to whole
        /// chips so total chips in the game never inflate from rounding.
        ///
        /// DESIGN KNOB (Eric): rounding policy. Floor is house-safe. If you'd rather be
        /// player-friendly, switch to Math.Round(gross, MidpointRounding.AwayFromZero).
        /// </summary>
        public static long WinningReturn(long stake, Odds odds)
        {
            if (stake <= 0) throw new ArgumentOutOfRangeException(nameof(stake), "Stake must be positive.");
            decimal gross = stake * odds.Decimal;
            return (long)Math.Floor(gross);
        }

        /// <summary>Profit on a winning wager (return minus the original stake).</summary>
        public static long Profit(long stake, Odds odds) => WinningReturn(stake, odds) - stake;
    }
}
