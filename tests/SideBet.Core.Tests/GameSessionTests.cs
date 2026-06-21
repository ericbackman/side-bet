using System;
using System.Collections.Generic;
using System.Linq;
using SideBet.Core;
using Xunit;

namespace SideBet.Core.Tests
{
    public class GameSessionTests
    {
        private static Func<int> Sequence(params int[] rolls)
        {
            var queue = new Queue<int>(rolls);
            return () => queue.Dequeue();
        }

        private static GameSession TwoPlayerSession(long chips = 1000) =>
            new GameSession(new[]
            {
                new Player("alice", "Alice", chips),
                new Player("bob", "Bob", chips),
            });

        private static DiceDuel Duel(Func<int> rolls) =>
            new DiceDuel("A", "Alice", "B", "Bob", rolls, Odds.FromDecimal(2.0m));

        [Fact]
        public void Bankrolls_persist_and_accumulate_across_rounds()
        {
            var session = TwoPlayerSession();

            // Round 1: dice (5,2) -> Alice ("A") wins. Both back Alice.
            var g1 = Duel(Sequence(5, 2));
            session.StartRound(g1);
            session.PlaceBet("alice", "A", 100);
            session.PlaceBet("bob", "A", 100);
            session.PlayAndSettle();
            Assert.Equal(1100, session.PlayerById("alice").Bankroll); // 900 + 200
            Assert.Equal(1100, session.PlayerById("bob").Bankroll);   // 900 + 200

            // Round 2: dice (2,5) -> Bob ("B") wins. Alice backs herself (loses), Bob backs himself (wins).
            var g2 = Duel(Sequence(2, 5));
            session.StartRound(g2);
            session.PlaceBet("alice", "A", 200);
            session.PlaceBet("bob", "B", 200);
            session.PlayAndSettle();
            Assert.Equal(900, session.PlayerById("alice").Bankroll);  // 1100 - 200
            Assert.Equal(1300, session.PlayerById("bob").Bankroll);   // 1100 - 200 + 400

            Assert.Equal(2, session.RoundNumber);
        }

        [Fact]
        public void Standings_rank_by_chips_richest_first()
        {
            var session = TwoPlayerSession();
            var g1 = Duel(Sequence(6, 1)); // Alice wins
            session.StartRound(g1);
            session.PlaceBet("alice", "A", 300);
            session.PlayAndSettle();

            var standings = session.Standings();
            Assert.Equal("alice", standings[0].Id); // 1000 - 300 + 600 = 1300
            Assert.Equal("bob", standings[1].Id);   // untouched 1000
        }

        [Fact]
        public void Cannot_start_a_round_while_one_is_in_progress()
        {
            var session = TwoPlayerSession();
            session.StartRound(Duel(Sequence(1, 2)));
            Assert.Throws<InvalidOperationException>(() => session.StartRound(Duel(Sequence(3, 4))));
        }

        [Fact]
        public void Cannot_bet_before_a_round_starts()
        {
            var session = TwoPlayerSession();
            Assert.Throws<InvalidOperationException>(() => session.PlaceBet("alice", "A", 100));
        }

        [Fact]
        public void Session_needs_at_least_two_players()
        {
            Assert.Throws<ArgumentException>(() =>
                new GameSession(new[] { new Player("solo", "Solo", 1000) }));
        }
    }
}
