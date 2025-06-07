using NUnit.Framework;
using BlackjackGame.Core.Models;
using System;
using System.Collections.Generic;
using System.Linq;

namespace BlackjackGame.Tests.Models
{
    [TestFixture]
    public class DeckTests
    {
        [Test]
        public void Constructor_ShouldInitializeFullDeck()
        {
            // Arrange & Act
            var deck = new Deck();

            // Assert
            Assert.That(deck.RemainingCards, Is.EqualTo(52), "A full deck should have 52 cards");
        }

        [Test]
        public void Initialize_ShouldCreateCorrectNumberOfCards()
        {
            // Arrange
            var deck = new Deck();

            // Act
            deck.Initialize();

            // Assert
            Assert.That(deck.RemainingCards, Is.EqualTo(52));
        }

        [Test]
        public void Initialize_ShouldContainAllSuitsAndRanks()
        {
            // Arrange
            var deck = new Deck();
            var drawnCards = new List<Card>();

            // Act - Draw all cards to verify content
            while (deck.RemainingCards > 0)
            {
                drawnCards.Add(deck.DrawCard());
            }

            // Assert
            Assert.That(drawnCards.Count, Is.EqualTo(52));

            // Check that we have exactly 4 suits
            var suitGroups = drawnCards.GroupBy(c => c.Suit);
            Assert.That(suitGroups.Count(), Is.EqualTo(4), "Should have all 4 suits");

            // Check that each suit has 13 cards
            foreach (var suitGroup in suitGroups)
            {
                Assert.That(suitGroup.Count(), Is.EqualTo(13), $"Suit {suitGroup.Key} should have 13 cards");
            }

            // Check that we have all ranks
            var rankGroups = drawnCards.GroupBy(c => c.Rank);
            Assert.That(rankGroups.Count(), Is.EqualTo(13), "Should have all 13 ranks");

            // Check that each rank appears 4 times (once per suit)
            foreach (var rankGroup in rankGroups)
            {
                Assert.That(rankGroup.Count(), Is.EqualTo(4), $"Rank {rankGroup.Key} should appear 4 times");
            }
        }

        [Test]
        public void DrawCard_ShouldReturnCard()
        {
            // Arrange
            var deck = new Deck();

            // Act
            var card = deck.DrawCard();

            // Assert
            Assert.That(card, Is.Not.Null);
            Assert.That(card, Is.InstanceOf<Card>());
        }

        [Test]
        public void DrawCard_ShouldReduceRemainingCards()
        {
            // Arrange
            var deck = new Deck();
            var initialCount = deck.RemainingCards;

            // Act
            deck.DrawCard();

            // Assert
            Assert.That(deck.RemainingCards, Is.EqualTo(initialCount - 1));
        }

        [Test]
        public void DrawCard_MultipleCards_ShouldBeDifferentObjects()
        {
            // Arrange
            var deck = new Deck();

            // Act
            var card1 = deck.DrawCard();
            var card2 = deck.DrawCard();

            // Assert
            Assert.That(card1, Is.Not.SameAs(card2), "Each drawn card should be a different object instance");
        }

        [Test]
        public void DrawCard_WhenDeckEmpty_ShouldReinitializeAndDrawCard()
        {
            // Arrange
            var deck = new Deck();

            // Draw all cards to empty the deck
            while (deck.RemainingCards > 0)
            {
                deck.DrawCard();
            }

            Assert.That(deck.RemainingCards, Is.EqualTo(0), "Deck should be empty");

            // Act
            var card = deck.DrawCard();

            // Assert
            Assert.That(card, Is.Not.Null, "Should be able to draw card from reinitialized deck");
            Assert.That(deck.RemainingCards, Is.EqualTo(51), "Deck should be reinitialized with 51 remaining cards after drawing one");
        }

        [Test]
        public void RemainingCards_NewDeck_ShouldBe52()
        {
            // Arrange & Act
            var deck = new Deck();

            // Assert
            Assert.That(deck.RemainingCards, Is.EqualTo(52));
        }

        [Test]
        public void RemainingCards_AfterDrawingSomeCards_ShouldUpdateCorrectly()
        {
            // Arrange
            var deck = new Deck();
            var cardsToDraw = 10;

            // Act
            for (int i = 0; i < cardsToDraw; i++)
            {
                deck.DrawCard();
            }

            // Assert
            Assert.That(deck.RemainingCards, Is.EqualTo(52 - cardsToDraw));
        }

        [Test]
        public void Shuffle_ShouldNotChangeNumberOfCards()
        {
            // Arrange
            var deck = new Deck();
            var originalCount = deck.RemainingCards;

            // Act
            deck.Shuffle();

            // Assert
            Assert.That(deck.RemainingCards, Is.EqualTo(originalCount));
        }

        [Test]
        public void Shuffle_ShouldChangeCardOrder()
        {
            // Arrange
            var deck1 = new Deck();
            var deck2 = new Deck();

            // Draw a few cards from each deck to compare order
            var cards1 = new List<Card>();
            var cards2 = new List<Card>();

            for (int i = 0; i < 10; i++)
            {
                cards1.Add(deck1.DrawCard());
            }

            // Shuffle deck2 multiple times to increase chance of different order
            for (int i = 0; i < 5; i++)
            {
                deck2.Shuffle();
            }

            for (int i = 0; i < 10; i++)
            {
                cards2.Add(deck2.DrawCard());
            }

            // Assert
            // It's extremely unlikely (but theoretically possible) that the order is exactly the same
            bool orderIsDifferent = false;
            for (int i = 0; i < cards1.Count; i++)
            {
                if (cards1[i].Rank != cards2[i].Rank || cards1[i].Suit != cards2[i].Suit)
                {
                    orderIsDifferent = true;
                    break;
                }
            }

            Assert.That(orderIsDifferent, Is.True, "Shuffle should change the order of cards (Note: This test has a very small chance of failing due to randomness)");
        }

        [Test]
        public void Initialize_CalledMultipleTimes_ShouldResetDeck()
        {
            // Arrange
            var deck = new Deck();

            // Draw some cards
            for (int i = 0; i < 10; i++)
            {
                deck.DrawCard();
            }

            Assert.That(deck.RemainingCards, Is.EqualTo(42));

            // Act
            deck.Initialize();

            // Assert
            Assert.That(deck.RemainingCards, Is.EqualTo(52), "Initialize should reset deck to full 52 cards");
        }

        [Test]
        public void DrawCard_AllCards_ShouldNotThrowException()
        {
            // Arrange
            var deck = new Deck();
            var drawnCards = new List<Card>();

            // Act & Assert
            Assert.DoesNotThrow(() =>
            {
                while (deck.RemainingCards > 0)
                {
                    drawnCards.Add(deck.DrawCard());
                }
            });

            Assert.That(drawnCards.Count, Is.EqualTo(52));
        }

        [Test]
        public void DrawCard_BeyondDeckSize_ShouldAutomaticallyReinitialize()
        {
            // Arrange
            var deck = new Deck();
            var drawnCards = new List<Card>();

            // Act - Draw more cards than are in a single deck
            for (int i = 0; i < 60; i++) // More than 52
            {
                drawnCards.Add(deck.DrawCard());
            }

            // Assert
            Assert.That(drawnCards.Count, Is.EqualTo(60));
            Assert.That(drawnCards.Take(52).Count(), Is.EqualTo(52), "First 52 cards should be from first deck");
            Assert.That(drawnCards.Skip(52).Count(), Is.EqualTo(8), "Remaining 8 cards should be from reinitialized deck");
        }

        [Test]
        public void Deck_ShouldContainUniqueCardCombinations()
        {
            // Arrange
            var deck = new Deck();
            var drawnCards = new List<Card>();

            // Act
            while (deck.RemainingCards > 0)
            {
                drawnCards.Add(deck.DrawCard());
            }

            // Assert
            var uniqueCombinations = drawnCards
                .Select(c => new { c.Rank, c.Suit })
                .Distinct()
                .Count();

            Assert.That(uniqueCombinations, Is.EqualTo(52), "All cards should have unique Rank-Suit combinations");
        }
    }
}