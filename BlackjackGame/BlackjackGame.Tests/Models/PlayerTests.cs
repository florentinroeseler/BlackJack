using NUnit.Framework;
using BlackjackGame.Core.Models;

namespace BlackjackGame.Tests.Models
{
    [TestFixture]
    public class PlayerTests
    {
        [Test]
        public void Constructor_WithName_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var player = new Player("TestPlayer");

            // Assert
            Assert.That(player.Name, Is.EqualTo("TestPlayer"));
            Assert.That(player.Balance, Is.EqualTo(1000), "Default balance should be 1000");
            Assert.That(player.CurrentBet, Is.EqualTo(0));
            Assert.That(player.Id, Is.EqualTo(string.Empty));
            Assert.That(player.Hand, Is.Not.Null);
            Assert.That(player.Hand.Cards.Count, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithNameAndBalance_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var player = new Player("TestPlayer", 500);

            // Assert
            Assert.That(player.Name, Is.EqualTo("TestPlayer"));
            Assert.That(player.Balance, Is.EqualTo(500));
            Assert.That(player.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void Constructor_WithZeroBalance_ShouldWork()
        {
            // Arrange & Act
            var player = new Player("TestPlayer", 0);

            // Assert
            Assert.That(player.Balance, Is.EqualTo(0));
        }

        [Test]
        public void Id_ShouldBeSettableAndGettable()
        {
            // Arrange
            var player = new Player("TestPlayer");

            // Act
            player.Id = "player123";

            // Assert
            Assert.That(player.Id, Is.EqualTo("player123"));
        }

        [Test]
        public void Id_SetToNull_ShouldSetToEmptyString()
        {
            // Arrange
            var player = new Player("TestPlayer");

            // Act
            player.Id = null;

            // Assert
            Assert.That(player.Id, Is.EqualTo(string.Empty));
        }

        [Test]
        public void PlaceBet_ValidAmount_ShouldDeductFromBalance()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);

            // Act
            player.PlaceBet(100);

            // Assert
            Assert.That(player.CurrentBet, Is.EqualTo(100));
            Assert.That(player.Balance, Is.EqualTo(900));
        }

        [Test]
        public void PlaceBet_AmountEqualToBalance_ShouldWork()
        {
            // Arrange
            var player = new Player("TestPlayer", 100);

            // Act
            player.PlaceBet(100);

            // Assert
            Assert.That(player.CurrentBet, Is.EqualTo(100));
            Assert.That(player.Balance, Is.EqualTo(0));
        }

        [Test]
        public void PlaceBet_AmountGreaterThanBalance_ShouldNotChangeBet()
        {
            // Arrange
            var player = new Player("TestPlayer", 100);

            // Act
            player.PlaceBet(150);

            // Assert
            Assert.That(player.CurrentBet, Is.EqualTo(0));
            Assert.That(player.Balance, Is.EqualTo(100));
        }

        [Test]
        public void PlaceBet_ZeroAmount_ShouldNotChangeBet()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);

            // Act
            player.PlaceBet(0);

            // Assert
            Assert.That(player.CurrentBet, Is.EqualTo(0));
            Assert.That(player.Balance, Is.EqualTo(1000));
        }

        [Test]
        public void PlaceBet_NegativeAmount_ShouldNotChangeBet()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);

            // Act
            player.PlaceBet(-50);

            // Assert
            Assert.That(player.CurrentBet, Is.EqualTo(0));
            Assert.That(player.Balance, Is.EqualTo(1000));
        }

        [Test]
        public void PlaceBet_MultipleBets_ShouldReplaceCurrentBet()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);
            player.PlaceBet(100);
            Assert.That(player.Balance, Is.EqualTo(900));
            Assert.That(player.CurrentBet, Is.EqualTo(100));

            // Act
            player.PlaceBet(200);

            // Assert
            Assert.That(player.CurrentBet, Is.EqualTo(200));
            Assert.That(player.Balance, Is.EqualTo(700), "Should deduct additional 200 from remaining balance");
        }

        [Test]
        public void Win_ShouldDoubleCurrentBetAndAddToBalance()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);
            player.PlaceBet(100);
            Assert.That(player.Balance, Is.EqualTo(900));

            // Act
            player.Win();

            // Assert
            Assert.That(player.Balance, Is.EqualTo(1100), "Should get back bet (100) + winnings (100) = +200");
            Assert.That(player.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void Win_WithDifferentBetAmounts_ShouldCalculateCorrectly()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);
            player.PlaceBet(50);

            // Act
            player.Win();

            // Assert
            Assert.That(player.Balance, Is.EqualTo(1050), "950 + (50 * 2) = 1050");
            Assert.That(player.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void WinBlackjack_ShouldPayOneAndHalfTimes()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);
            player.PlaceBet(100);
            Assert.That(player.Balance, Is.EqualTo(900));

            // Act
            player.WinBlackjack();

            // Assert
            Assert.That(player.Balance, Is.EqualTo(1150), "Should get back bet (100) + winnings (150) = +250");
            Assert.That(player.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void WinBlackjack_WithOddBetAmount_ShouldTruncateDecimal()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);
            player.PlaceBet(33); // 33 * 2.5 = 82.5, should truncate to 82

            // Act
            player.WinBlackjack();

            // Assert
            Assert.That(player.Balance, Is.EqualTo(1049), "967 + 82 = 1049 (33 * 2.5 truncated)");
            Assert.That(player.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void Lose_ShouldOnlyResetCurrentBet()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);
            player.PlaceBet(100);
            Assert.That(player.Balance, Is.EqualTo(900));

            // Act
            player.Lose();

            // Assert
            Assert.That(player.Balance, Is.EqualTo(900), "Balance should not change (bet was already deducted)");
            Assert.That(player.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void Push_ShouldReturnBetToBalance()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);
            player.PlaceBet(100);
            Assert.That(player.Balance, Is.EqualTo(900));

            // Act
            player.Push();

            // Assert
            Assert.That(player.Balance, Is.EqualTo(1000), "Should return to original balance");
            Assert.That(player.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void Hand_ShouldBeUniqueInstance()
        {
            // Arrange
            var player1 = new Player("Player1");
            var player2 = new Player("Player2");

            // Act & Assert
            Assert.That(player1.Hand, Is.Not.SameAs(player2.Hand), "Each player should have their own hand instance");
        }

        [Test]
        public void Hand_ShouldPersistThroughGameRounds()
        {
            // Arrange
            var player = new Player("TestPlayer");
            var originalHand = player.Hand;

            // Act - Simulate game actions
            player.PlaceBet(100);
            player.Win();

            // Assert
            Assert.That(player.Hand, Is.SameAs(originalHand), "Hand instance should remain the same");
        }

        [Test]
        public void GameFlow_CompleteBettingCycle_ShouldWorkCorrectly()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);

            // Act & Assert - Place bet
            player.PlaceBet(200);
            Assert.That(player.Balance, Is.EqualTo(800));
            Assert.That(player.CurrentBet, Is.EqualTo(200));

            // Win the round
            player.Win();
            Assert.That(player.Balance, Is.EqualTo(1200));
            Assert.That(player.CurrentBet, Is.EqualTo(0));

            // Place another bet
            player.PlaceBet(300);
            Assert.That(player.Balance, Is.EqualTo(900));
            Assert.That(player.CurrentBet, Is.EqualTo(300));

            // Lose this round
            player.Lose();
            Assert.That(player.Balance, Is.EqualTo(900));
            Assert.That(player.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void GameFlow_BlackjackWin_ShouldCalculateCorrectly()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);

            // Act
            player.PlaceBet(200);
            player.WinBlackjack();

            // Assert
            Assert.That(player.Balance, Is.EqualTo(1300), "800 + (200 * 2.5) = 1300");
        }

        [Test]
        public void GameFlow_PushAfterBet_ShouldReturnToOriginal()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);
            var originalBalance = player.Balance;

            // Act
            player.PlaceBet(150);
            player.Push();

            // Assert
            Assert.That(player.Balance, Is.EqualTo(originalBalance));
        }

        [Test]
        public void EdgeCase_WinWithNoBet_ShouldNotCrash()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);
            // No bet placed

            // Act & Assert
            Assert.DoesNotThrow(() => player.Win());
            Assert.That(player.Balance, Is.EqualTo(1000));
            Assert.That(player.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void EdgeCase_LoseWithNoBet_ShouldNotCrash()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);

            // Act & Assert
            Assert.DoesNotThrow(() => player.Lose());
            Assert.That(player.Balance, Is.EqualTo(1000));
        }

        [Test]
        public void EdgeCase_PushWithNoBet_ShouldNotCrash()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);

            // Act & Assert
            Assert.DoesNotThrow(() => player.Push());
            Assert.That(player.Balance, Is.EqualTo(1000));
        }

        [Test]
        public void Name_ShouldBeSettable()
        {
            // Arrange
            var player = new Player("OriginalName");

            // Act
            player.Name = "NewName";

            // Assert
            Assert.That(player.Name, Is.EqualTo("NewName"));
        }

        [Test]
        public void Balance_ShouldBeSettable()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);

            // Act
            player.Balance = 2000;

            // Assert
            Assert.That(player.Balance, Is.EqualTo(2000));
        }

        [Test]
        public void CurrentBet_ShouldBeSettable()
        {
            // Arrange
            var player = new Player("TestPlayer");

            // Act
            player.CurrentBet = 150;

            // Assert
            Assert.That(player.CurrentBet, Is.EqualTo(150));
        }
    }
}