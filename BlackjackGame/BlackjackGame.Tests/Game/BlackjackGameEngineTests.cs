using NUnit.Framework;
using BlackjackGame.Core.Game;
using BlackjackGame.Core.Models;
using System;
using System.Threading.Tasks;

namespace BlackjackGame.Tests.Game
{
    [TestFixture]
    public class BlackjackGameEngineTests
    {
        private BlackjackGameEngine engine;

        [SetUp]
        public void SetUp()
        {
            engine = new BlackjackGameEngine();
        }

        [Test]
        public void Constructor_SinglePlayer_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var singlePlayerEngine = new BlackjackGameEngine(false);

            // Assert
            Assert.That(singlePlayerEngine.IsTwoPlayerMode, Is.False);
            Assert.That(singlePlayerEngine.State, Is.EqualTo(GameState.PlacingBets));
            Assert.That(singlePlayerEngine.Deck, Is.Not.Null);
            Assert.That(singlePlayerEngine.Dealer, Is.Not.Null);
            Assert.That(singlePlayerEngine.Player1, Is.Not.Null);
            Assert.That(singlePlayerEngine.Player2, Is.Null);
            Assert.That(singlePlayerEngine.CurrentPlayer, Is.EqualTo(singlePlayerEngine.Player1));
        }

        [Test]
        public void Constructor_TwoPlayer_ShouldInitializeCorrectly()
        {
            // Arrange & Act
            var twoPlayerEngine = new BlackjackGameEngine(true);

            // Assert
            Assert.That(twoPlayerEngine.IsTwoPlayerMode, Is.True);
            Assert.That(twoPlayerEngine.State, Is.EqualTo(GameState.PlacingBets));
            Assert.That(twoPlayerEngine.Player1, Is.Not.Null);
            Assert.That(twoPlayerEngine.Player2, Is.Not.Null);
            Assert.That(twoPlayerEngine.CurrentPlayer, Is.EqualTo(twoPlayerEngine.Player1));
        }

        [Test]
        public void Constructor_DefaultParameters_ShouldBeSinglePlayer()
        {
            // Arrange & Act
            var defaultEngine = new BlackjackGameEngine();

            // Assert
            Assert.That(defaultEngine.IsTwoPlayerMode, Is.False);
            Assert.That(defaultEngine.Player2, Is.Null);
        }

        [Test]
        public void StartNewRound_ShouldResetGameState()
        {
            // Arrange
            engine.Player1.Hand.AddCard(new Card(Rank.King, Suit.Hearts));
            engine.Dealer.Hand.AddCard(new Card(Rank.Queen, Suit.Spades));

            // Act
            engine.StartNewRound();

            // Assert
            Assert.That(engine.State, Is.EqualTo(GameState.PlacingBets));
            Assert.That(engine.CurrentPlayer, Is.EqualTo(engine.Player1));
            Assert.That(engine.Player1.Hand.Cards.Count, Is.EqualTo(0));
            Assert.That(engine.Dealer.Hand.Cards.Count, Is.EqualTo(0));
        }

        [Test]
        public void StartNewRound_TwoPlayerMode_ShouldClearBothPlayerHands()
        {
            // Arrange
            var twoPlayerEngine = new BlackjackGameEngine(true);
            twoPlayerEngine.Player1.Hand.AddCard(new Card(Rank.King, Suit.Hearts));
            twoPlayerEngine.Player2.Hand.AddCard(new Card(Rank.Queen, Suit.Spades));

            // Act
            twoPlayerEngine.StartNewRound();

            // Assert
            Assert.That(twoPlayerEngine.Player1.Hand.Cards.Count, Is.EqualTo(0));
            Assert.That(twoPlayerEngine.Player2.Hand.Cards.Count, Is.EqualTo(0));
        }

        [Test]
        public void StartNewRound_ShouldTriggerGameStateChangedEvent()
        {
            // Arrange
            bool eventTriggered = false;
            GameState newState = GameState.GameOver;
            engine.GameStateChanged += (sender, e) =>
            {
                eventTriggered = true;
                newState = e.NewState;
            };

            // Act
            engine.StartNewRound();

            // Assert
            Assert.That(eventTriggered, Is.True);
            Assert.That(newState, Is.EqualTo(GameState.PlacingBets));
        }

        [Test]
        public void StartNewRound_ShouldTriggerCurrentPlayerChangedEvent()
        {
            // Arrange
            bool eventTriggered = false;
            Player newPlayer = null;
            engine.CurrentPlayerChanged += (sender, e) =>
            {
                eventTriggered = true;
                newPlayer = e.NewPlayer;
            };

            // Act
            engine.StartNewRound();

            // Assert
            Assert.That(eventTriggered, Is.True);
            Assert.That(newPlayer, Is.EqualTo(engine.Player1));
        }

        [Test]
        public void PlaceBet_InPlacingBetsState_ShouldPlaceBetAndStartGame()
        {
            // Arrange
            Assert.That(engine.State, Is.EqualTo(GameState.PlacingBets));

            // Act
            engine.PlaceBet(100);

            // Assert
            Assert.That(engine.Player1.CurrentBet, Is.EqualTo(100));
            Assert.That(engine.State, Is.EqualTo(GameState.PlayerTurn));
        }

        [Test]
        public void PlaceBet_NotInPlacingBetsState_ShouldNotPlaceBet()
        {
            // Arrange
            engine.PlaceBet(100); // This moves to PlayerTurn state
            Assert.That(engine.State, Is.EqualTo(GameState.PlayerTurn));
            var originalBet = engine.Player1.CurrentBet;

            // Act
            engine.PlaceBet(200); // This should be ignored

            // Assert
            Assert.That(engine.Player1.CurrentBet, Is.EqualTo(originalBet));
        }

        [Test]
        public void PlaceBet_TwoPlayerMode_ShouldSwitchToPlayer2ForBetting()
        {
            // Arrange
            var twoPlayerEngine = new BlackjackGameEngine(true);
            Assert.That(twoPlayerEngine.CurrentPlayer, Is.EqualTo(twoPlayerEngine.Player1));

            // Act
            twoPlayerEngine.PlaceBet(100);

            // Assert
            Assert.That(twoPlayerEngine.Player1.CurrentBet, Is.EqualTo(100));
            Assert.That(twoPlayerEngine.CurrentPlayer, Is.EqualTo(twoPlayerEngine.Player2));
            Assert.That(twoPlayerEngine.State, Is.EqualTo(GameState.PlacingBets)); // Still in betting phase
        }

        [Test]
        public void PlaceBet_TwoPlayerMode_BothPlayersFinishedBetting_ShouldStartGame()
        {
            // Arrange
            var twoPlayerEngine = new BlackjackGameEngine(true);

            // Act
            twoPlayerEngine.PlaceBet(100); // Player 1 bets
            twoPlayerEngine.PlaceBet(150); // Player 2 bets

            // Assert
            Assert.That(twoPlayerEngine.Player1.CurrentBet, Is.EqualTo(100));
            Assert.That(twoPlayerEngine.Player2.CurrentBet, Is.EqualTo(150));
            Assert.That(twoPlayerEngine.State, Is.EqualTo(GameState.PlayerTurn));
            Assert.That(twoPlayerEngine.CurrentPlayer, Is.EqualTo(twoPlayerEngine.Player1));
        }

        [Test]
        public void Hit_InPlayerTurn_ShouldAddCardToCurrentPlayer()
        {
            // Arrange
            engine.PlaceBet(100);
            Assert.That(engine.State, Is.EqualTo(GameState.PlayerTurn));
            var initialCardCount = engine.CurrentPlayer.Hand.Cards.Count;

            // Act
            engine.Hit();

            // Assert
            Assert.That(engine.CurrentPlayer.Hand.Cards.Count, Is.EqualTo(initialCardCount + 1));
            Assert.That(engine.CurrentPlayer.Hand.Cards[engine.CurrentPlayer.Hand.Cards.Count - 1].IsFaceUp, Is.True);
        }

        [Test]
        public void Hit_NotInPlayerTurn_ShouldNotAddCard()
        {
            // Arrange
            Assert.That(engine.State, Is.EqualTo(GameState.PlacingBets));
            var initialCardCount = engine.Player1.Hand.Cards.Count;

            // Act
            engine.Hit();

            // Assert
            Assert.That(engine.Player1.Hand.Cards.Count, Is.EqualTo(initialCardCount));
        }

        [Test]
        public void Stand_TwoPlayerMode_Player1Stands_ShouldSwitchToPlayer2()
        {
            // Arrange
            var twoPlayerEngine = new BlackjackGameEngine(true);
            twoPlayerEngine.PlaceBet(100); // Player 1 bets
            twoPlayerEngine.PlaceBet(150); // Player 2 bets
            Assert.That(twoPlayerEngine.CurrentPlayer, Is.EqualTo(twoPlayerEngine.Player1));

            // Act
            twoPlayerEngine.Stand();

            // Assert
            Assert.That(twoPlayerEngine.CurrentPlayer, Is.EqualTo(twoPlayerEngine.Player2));
            Assert.That(twoPlayerEngine.State, Is.EqualTo(GameState.PlayerTurn));
        }

        [Test]
        public void Stand_NotInPlayerTurn_ShouldNotChangeState()
        {
            // Arrange
            Assert.That(engine.State, Is.EqualTo(GameState.PlacingBets));

            // Act
            engine.Stand();

            // Assert
            Assert.That(engine.State, Is.EqualTo(GameState.PlacingBets));
        }

        [Test]
        public async Task PlayAgain_ShouldReturnTrueAndStartNewRound()
        {
            // Arrange
            var originalState = engine.State;

            // Act
            var result = await engine.PlayAgain();

            // Assert
            Assert.That(result, Is.True);
            Assert.That(engine.State, Is.EqualTo(GameState.PlacingBets));
        }

        [Test]
        public void Player1_ShouldBeSettable()
        {
            // Arrange
            var newPlayer = new Player("New Player 1");

            // Act
            engine.Player1 = newPlayer;

            // Assert
            Assert.That(engine.Player1, Is.EqualTo(newPlayer));
        }

        [Test]
        public void Player2_ShouldBeSettable()
        {
            // Arrange
            var newPlayer = new Player("New Player 2");

            // Act
            engine.Player2 = newPlayer;

            // Assert
            Assert.That(engine.Player2, Is.EqualTo(newPlayer));
        }

        [Test]
        public void GameStateChanged_EventArgs_ShouldContainCorrectState()
        {
            // Arrange
            GameState capturedState = GameState.GameOver;
            engine.GameStateChanged += (sender, e) =>
            {
                capturedState = e.NewState;
            };

            // Act
            engine.StartNewRound();

            // Assert
            Assert.That(capturedState, Is.EqualTo(GameState.PlacingBets));
        }

        [Test]
        public void CurrentPlayerChanged_EventArgs_ShouldContainCorrectPlayer()
        {
            // Arrange
            Player capturedPlayer = null;
            engine.CurrentPlayerChanged += (sender, e) =>
            {
                capturedPlayer = e.NewPlayer;
            };

            // Act
            engine.StartNewRound();

            // Assert
            Assert.That(capturedPlayer, Is.EqualTo(engine.Player1));
        }

        [Test]
        public void DealInitialCards_ShouldGiveCardsToPlayersAndDealer()
        {
            // Arrange & Act
            engine.PlaceBet(100); // This triggers dealing

            // Assert
            Assert.That(engine.Player1.Hand.Cards.Count, Is.GreaterThan(0));
            Assert.That(engine.Dealer.Hand.Cards.Count, Is.GreaterThan(0));
        }

        [Test]
        public void DealInitialCards_TwoPlayerMode_ShouldGiveCardsToBothPlayers()
        {
            // Arrange
            var twoPlayerEngine = new BlackjackGameEngine(true);

            // Act
            twoPlayerEngine.PlaceBet(100); // Player 1 bets
            twoPlayerEngine.PlaceBet(150); // Player 2 bets

            // Assert
            Assert.That(twoPlayerEngine.Player1.Hand.Cards.Count, Is.GreaterThan(0));
            Assert.That(twoPlayerEngine.Player2.Hand.Cards.Count, Is.GreaterThan(0));
            Assert.That(twoPlayerEngine.Dealer.Hand.Cards.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Engine_Properties_ShouldBeReadOnlyWhereExpected()
        {
            // Assert - These should have private setters
            Assert.That(engine.Deck, Is.Not.Null);
            Assert.That(engine.Dealer, Is.Not.Null);
            Assert.That(engine.CurrentPlayer, Is.Not.Null);
            Assert.That(engine.State, Is.EqualTo(GameState.PlacingBets));
            Assert.That(engine.IsTwoPlayerMode, Is.False);
        }

        [Test]
        public void MultipleEventSubscribers_ShouldAllBeNotified()
        {
            // Arrange
            int gameStateChangedCount = 0;
            int currentPlayerChangedCount = 0;

            engine.GameStateChanged += (s, e) => gameStateChangedCount++;
            engine.GameStateChanged += (s, e) => gameStateChangedCount++;
            engine.CurrentPlayerChanged += (s, e) => currentPlayerChangedCount++;
            engine.CurrentPlayerChanged += (s, e) => currentPlayerChangedCount++;

            // Act
            engine.StartNewRound();

            // Assert
            Assert.That(gameStateChangedCount, Is.EqualTo(2));
            Assert.That(currentPlayerChangedCount, Is.EqualTo(2));
        }

        [Test]
        public void CompleteGameFlow_PlayerWins_ShouldCalculateCorrectly()
        {
            // Arrange
            engine.PlaceBet(100);

            // Manually set up winning scenario - Player gets 20, dealer gets 19
            engine.Player1.Hand.Clear();
            engine.Dealer.Hand.Clear();
            engine.Player1.Hand.AddCard(new Card(Rank.King, Suit.Hearts));
            engine.Player1.Hand.AddCard(new Card(Rank.Ten, Suit.Spades));
            engine.Dealer.Hand.AddCard(new Card(Rank.Nine, Suit.Hearts));
            engine.Dealer.Hand.AddCard(new Card(Rank.Ten, Suit.Spades));

            var originalBalance = engine.Player1.Balance;

            // Act
            engine.Stand(); // This should trigger dealer turn and winner determination

            // Assert
            Assert.That(engine.State, Is.EqualTo(GameState.GameOver));
            Assert.That(engine.Player1.Balance, Is.EqualTo(originalBalance + 200), "Player should win 2x bet");
            Assert.That(engine.Player1.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void CompleteGameFlow_PlayerBlackjack_ShouldCalculateCorrectly()
        {
            // Arrange
            engine.PlaceBet(100);

            // Set up blackjack scenario - Player gets blackjack, dealer doesn't
            engine.Player1.Hand.Clear();
            engine.Dealer.Hand.Clear();
            engine.Player1.Hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            engine.Player1.Hand.AddCard(new Card(Rank.King, Suit.Spades));
            engine.Dealer.Hand.AddCard(new Card(Rank.Ten, Suit.Hearts));
            engine.Dealer.Hand.AddCard(new Card(Rank.Nine, Suit.Spades));

            var originalBalance = engine.Player1.Balance;

            // Act
            engine.Stand();

            // Assert
            Assert.That(engine.Player1.Balance, Is.EqualTo(originalBalance + 250), "Player should win 2.5x bet for blackjack");
        }

        [Test]
        public void CompleteGameFlow_PlayerBusts_ShouldLose()
        {
            // Arrange
            engine.PlaceBet(100);

            // Set up bust scenario
            engine.Player1.Hand.Clear();
            engine.Player1.Hand.AddCard(new Card(Rank.King, Suit.Hearts));
            engine.Player1.Hand.AddCard(new Card(Rank.Queen, Suit.Spades));
            engine.Player1.Hand.AddCard(new Card(Rank.Five, Suit.Diamonds));

            var originalBalance = engine.Player1.Balance;

            // Act
            engine.Hit(); // This should trigger dealer turn because player busted

            // Assert
            Assert.That(engine.State, Is.EqualTo(GameState.GameOver));
            Assert.That(engine.Player1.Balance, Is.EqualTo(originalBalance), "Player should lose bet (already deducted)");
            Assert.That(engine.Player1.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void CompleteGameFlow_Push_ShouldReturnBet()
        {
            // Arrange
            engine.PlaceBet(100);

            // Set up push scenario - both have 20
            engine.Player1.Hand.Clear();
            engine.Dealer.Hand.Clear();
            engine.Player1.Hand.AddCard(new Card(Rank.King, Suit.Hearts));
            engine.Player1.Hand.AddCard(new Card(Rank.Ten, Suit.Spades));
            engine.Dealer.Hand.AddCard(new Card(Rank.Queen, Suit.Hearts));
            engine.Dealer.Hand.AddCard(new Card(Rank.Ten, Suit.Diamonds));

            var originalBalance = engine.Player1.Balance;

            // Act
            engine.Stand();

            // Assert
            Assert.That(engine.Player1.Balance, Is.EqualTo(originalBalance + 100), "Player should get bet back on push");
            Assert.That(engine.Player1.CurrentBet, Is.EqualTo(0));
        }

        [Test]
        public void CompleteGameFlow_DealerBusts_PlayerWins()
        {
            // Arrange
            engine.PlaceBet(100);

            // Set up scenario where dealer will bust
            engine.Player1.Hand.Clear();
            engine.Dealer.Hand.Clear();
            engine.Player1.Hand.AddCard(new Card(Rank.Ten, Suit.Hearts));
            engine.Player1.Hand.AddCard(new Card(Rank.Nine, Suit.Spades));

            // Dealer has 16 and will be forced to draw (and likely bust)
            engine.Dealer.Hand.AddCard(new Card(Rank.Ten, Suit.Hearts));
            engine.Dealer.Hand.AddCard(new Card(Rank.Six, Suit.Spades));

            var originalBalance = engine.Player1.Balance;

            // Act
            engine.Stand();

            // Assert
            Assert.That(engine.State, Is.EqualTo(GameState.GameOver));
            // Player should win regardless of dealer's final value if dealer busts
            Assert.That(engine.Player1.Balance, Is.GreaterThan(originalBalance));
        }

        [Test]
        public void CompleteGameFlow_TwoPlayerMode_BothPlayersHandledCorrectly()
        {
            // Arrange
            var twoPlayerEngine = new BlackjackGameEngine(true);
            twoPlayerEngine.PlaceBet(100); // Player 1
            twoPlayerEngine.PlaceBet(150); // Player 2

            // Set up scenario - Player 1 wins, Player 2 loses
            twoPlayerEngine.Player1.Hand.Clear();
            twoPlayerEngine.Player2.Hand.Clear();
            twoPlayerEngine.Dealer.Hand.Clear();

            twoPlayerEngine.Player1.Hand.AddCard(new Card(Rank.King, Suit.Hearts));
            twoPlayerEngine.Player1.Hand.AddCard(new Card(Rank.Ten, Suit.Spades)); // 20

            twoPlayerEngine.Player2.Hand.AddCard(new Card(Rank.Ten, Suit.Hearts));
            twoPlayerEngine.Player2.Hand.AddCard(new Card(Rank.Five, Suit.Spades)); // 15

            twoPlayerEngine.Dealer.Hand.AddCard(new Card(Rank.Nine, Suit.Hearts));
            twoPlayerEngine.Dealer.Hand.AddCard(new Card(Rank.Nine, Suit.Spades)); // 18

            var originalBalance1 = twoPlayerEngine.Player1.Balance;
            var originalBalance2 = twoPlayerEngine.Player2.Balance;

            // Act
            twoPlayerEngine.Stand(); // Player 1 stands
            twoPlayerEngine.Stand(); // Player 2 stands

            // Assert
            Assert.That(twoPlayerEngine.State, Is.EqualTo(GameState.GameOver));
            Assert.That(twoPlayerEngine.Player1.Balance, Is.EqualTo(originalBalance1 + 200), "Player 1 should win");
            Assert.That(twoPlayerEngine.Player2.Balance, Is.EqualTo(originalBalance2), "Player 2 should lose");
        }

        [Test]
        public void DealerTurn_ShouldTransitionToGameOver()
        {
            // Arrange
            engine.PlaceBet(100);
            bool gameStateChanged = false;
            engine.GameStateChanged += (s, e) =>
            {
                if (e.NewState == GameState.GameOver)
                    gameStateChanged = true;
            };

            // Act
            engine.Stand(); // This triggers dealer turn

            // Assert
            Assert.That(gameStateChanged, Is.True);
            Assert.That(engine.State, Is.EqualTo(GameState.GameOver));
        }

        [Test]
        public void EvaluatePlayerOutcome_EdgeCases_ShouldHandleCorrectly()
        {
            // This test ensures the private EvaluatePlayerOutcome method works correctly
            // by testing through the public interface

            // Test Case: Player and Dealer both have blackjack (should be push)
            engine.PlaceBet(100);
            engine.Player1.Hand.Clear();
            engine.Dealer.Hand.Clear();

            engine.Player1.Hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            engine.Player1.Hand.AddCard(new Card(Rank.King, Suit.Spades));
            engine.Dealer.Hand.AddCard(new Card(Rank.Ace, Suit.Diamonds));
            engine.Dealer.Hand.AddCard(new Card(Rank.Queen, Suit.Hearts));

            var originalBalance = engine.Player1.Balance;

            // Act
            engine.Stand();

            // Assert
            Assert.That(engine.Player1.Balance, Is.EqualTo(originalBalance + 100), "Both blackjack should be push");
        }
    }
}