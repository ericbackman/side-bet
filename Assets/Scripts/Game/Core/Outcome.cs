namespace SideBet.Core
{
    /// <summary>A bettable outcome in a market, e.g. "Alice wins", with its offered odds.</summary>
    public sealed class Outcome
    {
        public string Id { get; }
        public string Label { get; }
        public Odds Odds { get; }

        public Outcome(string id, string label, Odds odds)
        {
            Id = id;
            Label = label;
            Odds = odds;
        }
    }
}
