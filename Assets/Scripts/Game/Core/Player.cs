using System;

namespace SideBet.Core
{
    /// <summary>A bettor with a chip bankroll. The bankroll can never go negative.</summary>
    public sealed class Player
    {
        public string Id { get; }
        public string Name { get; }
        public long Bankroll { get; private set; }

        public Player(string id, string name, long startingChips)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("id required", nameof(id));
            if (startingChips < 0) throw new ArgumentOutOfRangeException(nameof(startingChips));
            Id = id;
            Name = name;
            Bankroll = startingChips;
        }

        public bool CanAfford(long chips) => chips > 0 && chips <= Bankroll;

        public void Debit(long chips)
        {
            if (!CanAfford(chips))
                throw new InvalidOperationException(
                    $"{Name} cannot debit {chips} from bankroll {Bankroll}.");
            Bankroll -= chips;
        }

        public void Credit(long chips)
        {
            if (chips < 0) throw new ArgumentOutOfRangeException(nameof(chips));
            Bankroll += chips;
        }
    }
}
