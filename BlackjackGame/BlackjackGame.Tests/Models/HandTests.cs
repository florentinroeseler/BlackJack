using NUnit.Framework;
using BlackjackGame.Core.Models;
using System.Linq;

namespace BlackjackGame.Tests.Models
{
    [TestFixture]
    public class HandTests
    {
        private Hand hand;

        [SetUp]
        public void SetUp()
        {
            hand = new Hand();
        }

        [Test]
        public void Constructor_ShouldInitializeEmptyHand()
        {
            // Assert
            Assert.That(hand.Cards.Count, Is.EqualTo(0));
            Assert.That(hand.GetValue(), Is.EqualTo(0));
            Assert.That(hand.IsBusted, Is.False);
            Assert.That(hand.HasBlackjack, Is.False);
        }

        [Test]
        public void AddCard_ShouldAddCardToHand()
        {
            // Arrange
            var card = new Card(Rank.King, Suit.Hearts);

            // Act
            hand.AddCard(card);

            // Assert
            Assert.That(hand.Cards.Count, Is.EqualTo(1));
            Assert.That(hand.Cards[0], Is.EqualTo(card));
        }

        [Test]
        public void AddCard_ShouldSetCardFaceUp()
        {
            // Arrange
            var card = new Card(Rank.King, Suit.Hearts);
            Assert.That(card.IsFaceUp, Is.False, "Card should start face down");

            // Act
            hand.AddCard(card);

            // Assert
            Assert.That(card.IsFaceUp, Is.True, "Card should be face up after adding to hand");
        }

        [Test]
        public void Clear_ShouldRemoveAllCards()
        {
            // Arrange
            hand.AddCard(new Card(Rank.King, Suit.Hearts));
            hand.AddCard(new Card(Rank.Queen, Suit.Spades));
            Assert.That(hand.Cards.Count, Is.EqualTo(2));

            // Act
            hand.Clear();

            // Assert
            Assert.That(hand.Cards.Count, Is.EqualTo(0));
            Assert.That(hand.GetValue(), Is.EqualTo(0));
        }

        [Test]
        [TestCase(Rank.Two, 2)]
        [TestCase(Rank.Five, 5)]
        [TestCase(Rank.Nine, 9)]
        [TestCase(Rank.Ten, 10)]
        public void GetValue_SingleNumberCard_ShouldReturnCorrectValue(Rank rank, int expectedValue)
        {
            // Arrange
            hand.AddCard(new Card(rank, Suit.Hearts));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(Rank.Jack)]
        [TestCase(Rank.Queen)]
        [TestCase(Rank.King)]
        public void GetValue_SingleFaceCard_ShouldReturnTen(Rank rank)
        {
            // Arrange
            hand.AddCard(new Card(rank, Suit.Hearts));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(10));
        }

        [Test]
        public void GetValue_SingleAce_ShouldReturnEleven()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(11));
        }

        [Test]
        public void GetValue_AceAndTen_ShouldReturnTwentyOne()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.Ten, Suit.Spades));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(21));
        }

        [Test]
        public void GetValue_AceAndFaceCard_ShouldReturnTwentyOne()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.King, Suit.Spades));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(21));
        }

        [Test]
        public void GetValue_TwoAces_ShouldReturnTwelve()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.Ace, Suit.Spades));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(12), "Two aces should be 11 + 1 = 12");
        }

        [Test]
        public void GetValue_ThreeAces_ShouldReturnThirteen()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.Ace, Suit.Spades));
            hand.AddCard(new Card(Rank.Ace, Suit.Diamonds));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(13), "Three aces should be 11 + 1 + 1 = 13");
        }

        [Test]
        public void GetValue_FourAces_ShouldReturnFourteen()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.Ace, Suit.Spades));
            hand.AddCard(new Card(Rank.Ace, Suit.Diamonds));
            hand.AddCard(new Card(Rank.Ace, Suit.Clubs));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(14), "Four aces should be 11 + 1 + 1 + 1 = 14");
        }

        [Test]
        public void GetValue_AceWithCardsOverTwentyOne_ShouldCountAceAsOne()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.King, Suit.Spades));
            hand.AddCard(new Card(Rank.Five, Suit.Diamonds));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(16), "Ace + King + 5 should be 1 + 10 + 5 = 16 (Ace counted as 1)");
        }

        [Test]
        public void GetValue_MultipleAcesWithBust_ShouldAdjustAllNecessaryAces()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.Ace, Suit.Spades));
            hand.AddCard(new Card(Rank.Nine, Suit.Diamonds));
            hand.AddCard(new Card(Rank.Five, Suit.Clubs));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(16), "Two Aces + 9 + 5 should be 1 + 1 + 9 + 5 = 16");
        }

        [Test]
        public void GetValue_ComplexAceScenario_ShouldOptimizeValue()
        {
            // Arrange - Scenario: Ace, 6, Ace, 3 = should be 21 (11 + 6 + 1 + 3)
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.Six, Suit.Spades));
            hand.AddCard(new Card(Rank.Ace, Suit.Diamonds));
            hand.AddCard(new Card(Rank.Three, Suit.Clubs));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(21));
        }

        [Test]
        public void IsBusted_ValueUnderTwentyTwo_ShouldReturnFalse()
        {
            // Arrange
            hand.AddCard(new Card(Rank.King, Suit.Hearts));
            hand.AddCard(new Card(Rank.Queen, Suit.Spades));

            // Act & Assert
            Assert.That(hand.IsBusted, Is.False);
            Assert.That(hand.GetValue(), Is.EqualTo(20));
        }

        [Test]
        public void IsBusted_ValueEqualsTwentyOne_ShouldReturnFalse()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.King, Suit.Spades));

            // Act & Assert
            Assert.That(hand.IsBusted, Is.False);
            Assert.That(hand.GetValue(), Is.EqualTo(21));
        }

        [Test]
        public void IsBusted_ValueOverTwentyOne_ShouldReturnTrue()
        {
            // Arrange
            hand.AddCard(new Card(Rank.King, Suit.Hearts));
            hand.AddCard(new Card(Rank.Queen, Suit.Spades));
            hand.AddCard(new Card(Rank.Five, Suit.Diamonds));

            // Act & Assert
            Assert.That(hand.IsBusted, Is.True);
            Assert.That(hand.GetValue(), Is.EqualTo(25));
        }

        [Test]
        public void HasBlackjack_AceAndTen_ShouldReturnTrue()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.Ten, Suit.Spades));

            // Act & Assert
            Assert.That(hand.HasBlackjack, Is.True);
            Assert.That(hand.GetValue(), Is.EqualTo(21));
            Assert.That(hand.Cards.Count, Is.EqualTo(2));
        }

        [Test]
        public void HasBlackjack_AceAndFaceCard_ShouldReturnTrue()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.King, Suit.Spades));

            // Act & Assert
            Assert.That(hand.HasBlackjack, Is.True);
        }

        [Test]
        public void HasBlackjack_TwentyOneWithThreeCards_ShouldReturnFalse()
        {
            // Arrange
            hand.AddCard(new Card(Rank.Seven, Suit.Hearts));
            hand.AddCard(new Card(Rank.Seven, Suit.Spades));
            hand.AddCard(new Card(Rank.Seven, Suit.Diamonds));

            // Act & Assert
            Assert.That(hand.HasBlackjack, Is.False);
            Assert.That(hand.GetValue(), Is.EqualTo(21));
            Assert.That(hand.Cards.Count, Is.EqualTo(3));
        }

        [Test]
        public void HasBlackjack_TwoCardsNotTwentyOne_ShouldReturnFalse()
        {
            // Arrange
            hand.AddCard(new Card(Rank.King, Suit.Hearts));
            hand.AddCard(new Card(Rank.Queen, Suit.Spades));

            // Act & Assert
            Assert.That(hand.HasBlackjack, Is.False);
            Assert.That(hand.GetValue(), Is.EqualTo(20));
        }

        [Test]
        public void Cards_ShouldBeReadOnly()
        {
            // Arrange
            hand.AddCard(new Card(Rank.King, Suit.Hearts));
            var cards = hand.Cards;

            // Act & Assert
            Assert.That(cards, Is.InstanceOf<System.Collections.ObjectModel.ReadOnlyCollection<Card>>());

            // Should not be able to modify the collection directly
            Assert.Throws<System.NotSupportedException>(() =>
            {
                ((System.Collections.Generic.ICollection<Card>)cards).Add(new Card(Rank.Ace, Suit.Spades));
            });
        }

        [Test]
        public void GetValue_AfterClear_ShouldReturnZero()
        {
            // Arrange
            hand.AddCard(new Card(Rank.King, Suit.Hearts));
            hand.AddCard(new Card(Rank.Queen, Suit.Spades));
            Assert.That(hand.GetValue(), Is.EqualTo(20));

            // Act
            hand.Clear();

            // Assert
            Assert.That(hand.GetValue(), Is.EqualTo(0));
        }

        [Test]
        public void Hand_Properties_ShouldBeConsistent()
        {
            // Test that all properties work together correctly

            // Empty hand
            Assert.That(hand.GetValue(), Is.EqualTo(0));
            Assert.That(hand.IsBusted, Is.False);
            Assert.That(hand.HasBlackjack, Is.False);

            // Add Ace
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            Assert.That(hand.GetValue(), Is.EqualTo(11));
            Assert.That(hand.IsBusted, Is.False);
            Assert.That(hand.HasBlackjack, Is.False);

            // Add King for Blackjack
            hand.AddCard(new Card(Rank.King, Suit.Spades));
            Assert.That(hand.GetValue(), Is.EqualTo(21));
            Assert.That(hand.IsBusted, Is.False);
            Assert.That(hand.HasBlackjack, Is.True);

            // Add another card - no longer blackjack, potentially busted
            hand.AddCard(new Card(Rank.Five, Suit.Diamonds));
            Assert.That(hand.GetValue(), Is.EqualTo(16)); // Ace converts to 1
            Assert.That(hand.IsBusted, Is.False);
            Assert.That(hand.HasBlackjack, Is.False);
        }

        [Test]
        public void GetValue_EdgeCase_AllTensAndAce()
        {
            // Arrange - scenario where ace adjustment is critical
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.Ten, Suit.Spades));
            hand.AddCard(new Card(Rank.Ten, Suit.Diamonds));

            // Act
            var value = hand.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(21), "Ace + 10 + 10 should be 1 + 10 + 10 = 21");
            Assert.That(hand.IsBusted, Is.False);
            Assert.That(hand.HasBlackjack, Is.False, "Three cards cannot be blackjack");
        }
    }
}