using NUnit.Framework;
using BlackjackGame.Core.Models;

namespace BlackjackGame.Tests.Models
{
    [TestFixture]
    public class CardTests
    {
        [Test]
        public void Constructor_ShouldInitializeCardCorrectly()
        {
            // Arrange & Act
            var card = new Card(Rank.Ace, Suit.Hearts);

            // Assert
            Assert.That(card.Rank, Is.EqualTo(Rank.Ace));
            Assert.That(card.Suit, Is.EqualTo(Suit.Hearts));
            Assert.That(card.IsFaceUp, Is.False, "Card should be face down by default");
        }

        [Test]
        [TestCase(Rank.Two, 2)]
        [TestCase(Rank.Three, 3)]
        [TestCase(Rank.Four, 4)]
        [TestCase(Rank.Five, 5)]
        [TestCase(Rank.Six, 6)]
        [TestCase(Rank.Seven, 7)]
        [TestCase(Rank.Eight, 8)]
        [TestCase(Rank.Nine, 9)]
        [TestCase(Rank.Ten, 10)]
        public void GetValue_NumberCards_ShouldReturnCorrectValue(Rank rank, int expectedValue)
        {
            // Arrange
            var card = new Card(rank, Suit.Hearts);

            // Act
            var value = card.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(expectedValue));
        }

        [Test]
        [TestCase(Rank.Jack)]
        [TestCase(Rank.Queen)]
        [TestCase(Rank.King)]
        public void GetValue_FaceCards_ShouldReturnTen(Rank rank)
        {
            // Arrange
            var card = new Card(rank, Suit.Hearts);

            // Act
            var value = card.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(10), $"{rank} should have value 10");
        }

        [Test]
        public void GetValue_Ace_ShouldReturnEleven()
        {
            // Arrange
            var card = new Card(Rank.Ace, Suit.Hearts);

            // Act
            var value = card.GetValue();

            // Assert
            Assert.That(value, Is.EqualTo(11), "Ace should initially be valued as 11");
        }

        [Test]
        [TestCase(Suit.Hearts)]
        [TestCase(Suit.Diamonds)]
        [TestCase(Suit.Clubs)]
        [TestCase(Suit.Spades)]
        public void Constructor_AllSuits_ShouldWork(Suit suit)
        {
            // Arrange & Act
            var card = new Card(Rank.King, suit);

            // Assert
            Assert.That(card.Suit, Is.EqualTo(suit));
        }

        [Test]
        public void IsFaceUp_ShouldBeSettable()
        {
            // Arrange
            var card = new Card(Rank.Ace, Suit.Hearts);
            Assert.That(card.IsFaceUp, Is.False, "Initial state should be face down");

            // Act
            card.IsFaceUp = true;

            // Assert
            Assert.That(card.IsFaceUp, Is.True);
        }

        [Test]
        [TestCase(Rank.Ace, Suit.Hearts, "Ace of Hearts")]
        [TestCase(Rank.King, Suit.Spades, "King of Spades")]
        [TestCase(Rank.Two, Suit.Diamonds, "Two of Diamonds")]
        [TestCase(Rank.Ten, Suit.Clubs, "Ten of Clubs")]
        public void ToString_ShouldReturnCorrectFormat(Rank rank, Suit suit, string expected)
        {
            // Arrange
            var card = new Card(rank, suit);

            // Act
            var result = card.ToString();

            // Assert
            Assert.That(result, Is.EqualTo(expected));
        }

        [Test]
        public void Card_ShouldBeImmutableExceptForIsFaceUp()
        {
            // Arrange
            var card = new Card(Rank.Ace, Suit.Hearts);
            var originalRank = card.Rank;
            var originalSuit = card.Suit;

            // Act & Assert
            // Rank und Suit sollten nur getter haben (readonly)
            Assert.That(card.Rank, Is.EqualTo(originalRank));
            Assert.That(card.Suit, Is.EqualTo(originalSuit));

            // IsFaceUp sollte änderbar sein
            card.IsFaceUp = !card.IsFaceUp;
            Assert.That(card.IsFaceUp, Is.Not.EqualTo(false));
        }

        [Test]
        public void GetValue_ConsistentResults_SameCardShouldAlwaysReturnSameValue()
        {
            // Arrange
            var card = new Card(Rank.Queen, Suit.Hearts);

            // Act
            var value1 = card.GetValue();
            var value2 = card.GetValue();
            var value3 = card.GetValue();

            // Assert
            Assert.That(value1, Is.EqualTo(value2));
            Assert.That(value2, Is.EqualTo(value3));
            Assert.That(value1, Is.EqualTo(10));
        }

        [Test]
        public void IsFaceUp_DoesNotAffectValue()
        {
            // Arrange
            var card = new Card(Rank.Ace, Suit.Hearts);
            var valueWhenFaceDown = card.GetValue();

            // Act
            card.IsFaceUp = true;
            var valueWhenFaceUp = card.GetValue();

            // Assert
            Assert.That(valueWhenFaceUp, Is.EqualTo(valueWhenFaceDown),
                "Card value should not change based on face up/down state");
        }
    }
}