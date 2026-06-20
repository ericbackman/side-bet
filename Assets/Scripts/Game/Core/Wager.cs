namespace SideBet.Core
{
    /// <summary>
    /// A placed bet: a player stakes chips on an outcome at the odds locked in at the moment
    /// they bet (sportsbook style — later odds moves don't change an existing ticket).
    /// </summary>
    public sealed class Wager
    {
        public string PlayerId { get; }
        public string OutcomeId { get; }
        public long Stake { get; }
        public Odds OddsAtPlacement { get; }

        public Wager(string playerId, string outcomeId, long stake, Odds oddsAtPlacement)
        {
            PlayerId = playerId;
            OutcomeId = outcomeId;
            Stake = stake;
            OddsAtPlacement = oddsAtPlacement;
        }
    }
}
