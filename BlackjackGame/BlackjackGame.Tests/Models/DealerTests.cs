using NUnit.Framework;
using BlackjackGame.Core.Models;
using System.Linq;

namespace BlackjackGame.Tests.Models
{
    [TestFixture]
    public class DealerTests
    {
        private Dealer dealer;
        private Deck deck;
        private Player player1;
        private Player player2;

        [SetUp]
        public void SetUp()
        {
            dealer = new Dealer();
            deck = new Deck();
            player1 = new Player("Player 1");
            player2 = new Player("Player 2");
        }

        [Test]
        public void Constructor_ShouldInitializeWithEmptyHand()
        {
            // Assert
            Assert.That(dealer.Hand, Is.Not.Null);
            Assert.That(dealer.Hand.Cards.Count, Is.EqualTo(0));
        }

        [Test]
        public void RevealHiddenCard_WithNoCards_ShouldNotThrowException()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => dealer.RevealHiddenCard());
        }

        [Test]
        public void RevealHiddenCard_WithAllFaceUpCards_ShouldRemainFaceUp()
        {
            // Arrange
            var card1 = new Card(Rank.King, Suit.Hearts);
            var card2 = new Card(Rank.Ace, Suit.Spades);
            card1.IsFaceUp = true;
            card2.IsFaceUp = true;

            dealer.Hand.AddCard(card1);
            dealer.Hand.AddCard(card2);

            // Act
            dealer.RevealHiddenCard();

            // Assert
            Assert.That(card1.IsFaceUp, Is.True);
            Assert.That(card2.IsFaceUp, Is.True);
        }

        [Test]
        public void DealInitialCards_SinglePlayer_ShouldDealCorrectNumberOfCards()
        {
            // Act
            dealer.DealInitialCards(deck, player1);

            // Assert
            Assert.That(player1.Hand.Cards.Count, Is.EqualTo(2));
            Assert.That(dealer.Hand.Cards.Count, Is.EqualTo(2));
        }

        [Test]
        public void DealInitialCards_TwoPlayers_ShouldDealCorrectNumberOfCards()
        {
            // Act
            dealer.DealInitialCards(deck, player1, player2);

            // Assert
            Assert.That(player1.Hand.Cards.Count, Is.EqualTo(2));
            Assert.That(player2.Hand.Cards.Count, Is.EqualTo(2));
            Assert.That(dealer.Hand.Cards.Count, Is.EqualTo(2));
        }

        [Test]
        public void DealInitialCards_ShouldReduceDeckSize()
        {
            // Arrange
            var initialDeckSize = deck.RemainingCards;

            // Act
            dealer.DealInitialCards(deck, player1);

            // Assert
            Assert.That(deck.RemainingCards, Is.EqualTo(initialDeckSize - 4), "Should draw 4 cards total");
        }

        [Test]
        public void DealInitialCards_TwoPlayers_ShouldReduceDeckSize()
        {
            // Arrange
            var initialDeckSize = deck.RemainingCards;

            // Act
            dealer.DealInitialCards(deck, player1, player2);

            // Assert
            Assert.That(deck.RemainingCards, Is.EqualTo(initialDeckSize - 6), "Should draw 6 cards total");
        }

        [Test]
        public void DealInitialCards_ShouldDealUniqueCards()
        {
            // Act
            dealer.DealInitialCards(deck, player1);

            // Assert
            var allCards = player1.Hand.Cards.Concat(dealer.Hand.Cards).ToList();
            var uniqueCards = allCards.Select(c => new { c.Rank, c.Suit }).Distinct().Count();

            Assert.That(uniqueCards, Is.EqualTo(4), "All dealt cards should be unique");
        }

        [Test]
        public void PlayTurn_DealerHas16_ShouldDrawCard()
        {
            // Arrange
            dealer.Hand.AddCard(new Card(Rank.Ten, Suit.Hearts));
            dealer.Hand.AddCard(new Card(Rank.Six, Suit.Spades));
            Assert.That(dealer.Hand.GetValue(), Is.EqualTo(16));
            var initialCardCount = dealer.Hand.Cards.Count;

            // Act
            dealer.PlayTurn(deck);

            // Assert
            Assert.That(dealer.Hand.Cards.Count, Is.GreaterThan(initialCardCount), "Dealer should draw at least one card");
            Assert.That(dealer.Hand.GetValue(), Is.GreaterThanOrEqualTo(17).Or.GreaterThan(21), "Dealer should reach 17+ or bust");
        }

        [Test]
        public void PlayTurn_DealerHas17_ShouldNotDrawCard()
        {
            // Arrange
            dealer.Hand.AddCard(new Card(Rank.Ten, Suit.Hearts));
            dealer.Hand.AddCard(new Card(Rank.Seven, Suit.Spades));
            Assert.That(dealer.Hand.GetValue(), Is.EqualTo(17));
            var initialCardCount = dealer.Hand.Cards.Count;

            // Act
            dealer.PlayTurn(deck);

            // Assert
            Assert.That(dealer.Hand.Cards.Count, Is.EqualTo(initialCardCount), "Dealer should not draw more cards with 17");
        }

        [Test]
        public void PlayTurn_DealerHas21_ShouldNotDrawCard()
        {
            // Arrange
            dealer.Hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            dealer.Hand.AddCard(new Card(Rank.King, Suit.Spades));
            Assert.That(dealer.Hand.GetValue(), Is.EqualTo(21));
            var initialCardCount = dealer.Hand.Cards.Count;

            // Act
            dealer.PlayTurn(deck);

            // Assert
            Assert.That(dealer.Hand.Cards.Count, Is.EqualTo(initialCardCount), "Dealer should not draw more cards with 21");
        }

        [Test]
        public void PlayTurn_DealerSoftAce_ShouldFollowRules()
        {
            // Arrange - Dealer has Ace + 6 = 17 (soft 17)
            dealer.Hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            dealer.Hand.AddCard(new Card(Rank.Six, Suit.Spades));
            Assert.That(dealer.Hand.GetValue(), Is.EqualTo(17));
            var initialCardCount = dealer.Hand.Cards.Count;

            // Act
            dealer.PlayTurn(deck);

            // Assert
            // Dealer should stop at soft 17 (this is typical casino rule)
            Assert.That(dealer.Hand.Cards.Count, Is.EqualTo(initialCardCount), "Dealer should stop at soft 17");
        }

        [Test]
        public void PlayTurn_DealerKeepsDrawingUntil17()
        {
            // Arrange
            dealer.Hand.AddCard(new Card(Rank.Two, Suit.Hearts));
            dealer.Hand.AddCard(new Card(Rank.Three, Suit.Spades));
            Assert.That(dealer.Hand.GetValue(), Is.EqualTo(5));

            // Act
            dealer.PlayTurn(deck);

            // Assert
            var finalValue = dealer.Hand.GetValue();
            Assert.That(finalValue, Is.GreaterThanOrEqualTo(17).Or.GreaterThan(21), "Dealer should reach 17+ or bust");
        }

        [Test]
        public void PlayTurn_AllNewCardsShouldBeFaceUp()
        {
            // Arrange
            dealer.Hand.AddCard(new Card(Rank.Two, Suit.Hearts));
            dealer.Hand.AddCard(new Card(Rank.Three, Suit.Spades));
            var initialCardCount = dealer.Hand.Cards.Count;

            // Act
            dealer.PlayTurn(deck);

            // Assert
            var newCards = dealer.Hand.Cards.Skip(initialCardCount);
            foreach (var card in newCards)
            {
                Assert.That(card.IsFaceUp, Is.True, "All newly drawn cards should be face up");
            }
        }

        [Test]
        public void PlayTurn_ShouldReduceDeckSize()
        {
            // Arrange
            dealer.Hand.AddCard(new Card(Rank.Two, Suit.Hearts));
            dealer.Hand.AddCard(new Card(Rank.Three, Suit.Spades));
            var initialDeckSize = deck.RemainingCards;
            var initialHandSize = dealer.Hand.Cards.Count;

            // Act
            dealer.PlayTurn(deck);

            // Assert
            var cardsDrawn = dealer.Hand.Cards.Count - initialHandSize;
            Assert.That(deck.RemainingCards, Is.EqualTo(initialDeckSize - cardsDrawn));
        }

        [Test]
        public void PlayTurn_DealerCanBust()
        {
            // Arrange - Force a scenario where dealer is likely to bust
            dealer.Hand.AddCard(new Card(Rank.Ten, Suit.Hearts));
            dealer.Hand.AddCard(new Card(Rank.Six, Suit.Spades));
            // Dealer has 16, must draw, could bust

            // Act
            dealer.PlayTurn(deck);

            // Assert
            // The dealer might bust (>21) or might get exactly 17-21
            // Both outcomes are valid, so we just check that the dealer followed the rules
            var finalValue = dealer.Hand.GetValue();
            Assert.That(finalValue >= 17 || dealer.Hand.IsBusted, Is.True,
                "Dealer should either reach 17+ or bust");
        }

        [Test]
        public void Hand_ShouldBeUniqueInstance()
        {
            // Arrange
            var dealer1 = new Dealer();
            var dealer2 = new Dealer();

            // Act & Assert
            Assert.That(dealer1.Hand, Is.Not.SameAs(dealer2.Hand), "Each dealer should have their own hand instance");
        }

        [Test]
        public void Hand_ShouldUseSameHandThroughoutGame()
        {
            // Arrange
            var originalHand = dealer.Hand;

            // Act
            dealer.DealInitialCards(deck, player1);
            dealer.PlayTurn(deck);

            // Assert
            Assert.That(dealer.Hand, Is.SameAs(originalHand), "Dealer should use the same hand instance throughout the game");
        }
    }
}