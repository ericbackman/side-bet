using System;
using System.Collections.Generic;
using System.Linq;
using SideBet.Core;
using Xunit;

namespace SideBet.Core.Tests
{
    public class BettingMarketTests
    {
        private static (BettingMarket market, Player alice, Player bob, Dictionary<string, Player> byId)
            EvenMoneyMarket(long chips = 1000)
        {
            var alice = new Player("alice", "Alice", chips);
            var bob = new Player("bob", "Bob", chips);
            var even = Odds.FromDecimal(2.0m);
            var market = new BettingMarket(new[]
            {
                new Outcome("A", "Alice wins", even),
                new Outcome("B", "Bob wins", even),
            });
            var byId = new[] { alice, bob }.ToDictionary(p => p.Id);
            return (market, alice, bob, byId);
        }

        [Fact]
        public void Placing_a_bet_debits_the_stake_immediately()
        {
            var (market, alice, _, _) = EvenMoneyMarket();
            market.PlaceBet(alice, "A", 100);
            Assert.Equal(900, alice.Bankroll);
            Assert.Single(market.Wagers);
        }

        [Fact]
        public void Cannot_bet_more_than_bankroll()
        {
            var (market, alice, _, _) = EvenMoneyMarket(50);
            Assert.Throws<InvalidOperationException>(() => market.PlaceBet(alice, "A", 100));
            Assert.Equal(50, alice.Bankroll); // unchanged
        }

        [Fact]
        public void Cannot_bet_after_lock()
        {
            var (market, alice, _, _) = EvenMoneyMarket();
            market.Lock();
            Assert.Throws<InvalidOperationException>(() => market.PlaceBet(alice, "A", 100));
        }

        [Fact]
        public void Settlement_pays_winners_and_keeps_loser_debit()
        {
            var (market, alice, bob, byId) = EvenMoneyMarket();
            market.PlaceBet(alice, "A", 100); // alice on herself
            market.PlaceBet(bob, "B", 100);   // bob on himself
            market.Lock();

            var net = market.Settle("A", byId); // Alice wins

            Assert.Equal(1100, alice.Bankroll); // 900 + 200 return
            Assert.Equal(900, bob.Bankroll);    // lost his 100
            Assert.Equal(100, net.Single(e => e.PlayerId == "alice").Net);
            Assert.Equal(-100, net.Single(e => e.PlayerId == "bob").Net);
        }

        [Fact]
        public void Even_money_head_to_head_conserves_total_chips()
        {
            var (market, alice, bob, byId) = EvenMoneyMarket();
            long before = alice.Bankroll + bob.Bankroll;
            market.PlaceBet(alice, "A", 250);
            market.PlaceBet(bob, "B", 250);
            market.Settle("B", byId);
            Assert.Equal(before, alice.Bankroll + bob.Bankroll); // zero-vig: chips conserved
        }

        [Fact]
        public void Sandbagging_is_allowed_bet_on_your_opponent_and_cash_out()
        {
            // The comedic core: a competitor bets on their OPPONENT and profits when they
            // "lose" the match. The betting core only knows playerId + outcomeId, so this
            // is naturally allowed — exactly the drama we want.
            var (market, alice, bob, byId) = EvenMoneyMarket();
            market.PlaceBet(alice, "B", 300); // Alice bets on Bob to win
            market.Settle("B", byId);         // Bob wins the match
            Assert.Equal(1300, alice.Bankroll); // Alice profits 300 by "throwing" it
        }

        [Fact]
        public void Cannot_settle_twice()
        {
            var (market, alice, _, byId) = EvenMoneyMarket();
            market.PlaceBet(alice, "A", 100);
            market.Settle("A", byId);
            Assert.Throws<InvalidOperationException>(() => market.Settle("A", byId));
        }
    }
}
