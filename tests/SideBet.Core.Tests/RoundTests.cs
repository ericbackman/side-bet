using System;
using System.Linq;
using SideBet.Core;
using Xunit;

namespace SideBet.Core.Tests
{
    public class RoundTests
    {
        private static Round NewRound(long chips = 1000) =>
            new Round(new[]
            {
                new Player("alice", "Alice", chips),
                new Player("bob", "Bob", chips),
            });

        private static Outcome[] EvenOutcomes() => new[]
        {
            new Outcome("A", "Alice wins", Odds.FromDecimal(2.0m)),
            new Outcome("B", "Bob wins", Odds.FromDecimal(2.0m)),
        };

        [Fact]
        public void Happy_path_moves_chips_correctly()
        {
            var round = NewRound();
            round.OpenBetting(EvenOutcomes());
            round.PlaceBet("alice", "A", 200);
            round.PlaceBet("bob", "A", 100); // Bob also backs Alice
            round.LockBetting();
            round.StartPlay();
            round.ResolveWith("A");
            var net = round.Settle();

            Assert.Equal(RoundPhase.Settled, round.Phase);
            Assert.Equal(1200, round.PlayerById("alice").Bankroll); // 800 + 400
            Assert.Equal(1100, round.PlayerById("bob").Bankroll);   // 900 - 100 stake + 200 return
            Assert.Equal(200, net.Single(e => e.PlayerId == "alice").Net);
            Assert.Equal(100, net.Single(e => e.PlayerId == "bob").Net);
        }

        [Fact]
        public void Cannot_bet_before_betting_opens()
        {
            var round = NewRound();
            Assert.Throws<InvalidOperationException>(() => round.PlaceBet("alice", "A", 100));
        }

        [Fact]
        public void Cannot_settle_before_resolved()
        {
            var round = NewRound();
            round.OpenBetting(EvenOutcomes());
            round.LockBetting();
            round.StartPlay();
            Assert.Throws<InvalidOperationException>(() => round.Settle());
        }

        [Fact]
        public void Cannot_resolve_with_unknown_outcome()
        {
            var round = NewRound();
            round.OpenBetting(EvenOutcomes());
            round.LockBetting();
            round.StartPlay();
            Assert.Throws<ArgumentException>(() => round.ResolveWith("does-not-exist"));
        }

        [Fact]
        public void Needs_at_least_two_players()
        {
            Assert.Throws<ArgumentException>(() => new Round(new[] { new Player("solo", "Solo", 1000) }));
        }
    }
}
