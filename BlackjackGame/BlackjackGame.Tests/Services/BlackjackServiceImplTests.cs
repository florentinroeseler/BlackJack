using NUnit.Framework;
using BlackjackGame.Server.Services;
using BlackjackGame.Core.Game;
using BlackjackGame.Core.Models;
using BlackjackGame.Core.Protos;
using Moq;
using System;
using System.Reflection;

namespace BlackjackGame.Tests.Services
{
    [TestFixture]
    public class BlackjackServiceImplTests
    {
        private BlackjackServiceImpl service;
        private Mock<GameManager> mockGameManager;
        private BlackjackGameEngine testGame;

        [SetUp]
        public void SetUp()
        {
            mockGameManager = new Mock<GameManager>();
            service = new BlackjackServiceImpl(mockGameManager.Object);
            testGame = new BlackjackGameEngine(false);
        }

        [Test]
        public void Constructor_ShouldInitializeWithGameManager()
        {
            // Arrange & Act
            var newService = new BlackjackServiceImpl(mockGameManager.Object);

            // Assert
            Assert.That(newService, Is.Not.Null);
        }

        [Test]
        public void MapGameState_AllStates_ShouldMapCorrectly()
        {
            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapGameState", BindingFlags.NonPublic | BindingFlags.Instance);

            // Test all enum values
            var placingBets = method.Invoke(service, new object[] { GameState.PlacingBets });
            var playerTurn = method.Invoke(service, new object[] { GameState.PlayerTurn });
            var dealerTurn = method.Invoke(service, new object[] { GameState.DealerTurn });
            var gameOver = method.Invoke(service, new object[] { GameState.GameOver });

            Assert.That(placingBets, Is.EqualTo(GameStateResponse.Types.GamePhase.PlacingBets));
            Assert.That(playerTurn, Is.EqualTo(GameStateResponse.Types.GamePhase.PlayerTurn));
            Assert.That(dealerTurn, Is.EqualTo(GameStateResponse.Types.GamePhase.DealerTurn));
            Assert.That(gameOver, Is.EqualTo(GameStateResponse.Types.GamePhase.GameOver));
        }

        [Test]
        public void GetStateMessage_AllStates_ShouldReturnCorrectMessages()
        {
            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("GetStateMessage", BindingFlags.NonPublic | BindingFlags.Instance);

            // Arrange - Set up test game in different states
            testGame.CurrentPlayer = testGame.Player1;
            testGame.Player1.Name = "TestPlayer";

            // Test PlacingBets
            SetGameState(testGame, GameState.PlacingBets);
            var placingBetsMessage = method.Invoke(service, new object[] { testGame });
            Assert.That(placingBetsMessage, Is.EqualTo("Place your bets"));

            // Test PlayerTurn
            SetGameState(testGame, GameState.PlayerTurn);
            var playerTurnMessage = method.Invoke(service, new object[] { testGame });
            Assert.That(playerTurnMessage, Is.EqualTo("TestPlayer's turn"));

            // Test DealerTurn
            SetGameState(testGame, GameState.DealerTurn);
            var dealerTurnMessage = method.Invoke(service, new object[] { testGame });
            Assert.That(dealerTurnMessage, Is.EqualTo("Dealer's turn"));

            // Test GameOver
            SetGameState(testGame, GameState.GameOver);
            var gameOverMessage = method.Invoke(service, new object[] { testGame });
            Assert.That(gameOverMessage, Is.EqualTo("Round over"));
        }

        [Test]
        public void MapPlayerInfo_ShouldMapAllProperties()
        {
            // Arrange
            var player = new Player("TestPlayer", 1000);
            player.Id = "player123";
            player.CurrentBet = 50;
            player.Hand.AddCard(new Card(Rank.King, Suit.Hearts));
            player.Hand.AddCard(new Card(Rank.Ace, Suit.Spades));

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapPlayerInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (PlayerInfo)method.Invoke(service, new object[] { player, true });

            // Assert
            Assert.That(result.Name, Is.EqualTo("TestPlayer"));
            Assert.That(result.Id, Is.EqualTo("player123"));
            Assert.That(result.Balance, Is.EqualTo(1000));
            Assert.That(result.CurrentBet, Is.EqualTo(50));
            Assert.That(result.IsCurrentPlayer, Is.True);
            Assert.That(result.Hand, Is.Not.Null);
            Assert.That(result.Hand.Value, Is.EqualTo(21));
            Assert.That(result.Hand.HasBlackjack, Is.True);
            Assert.That(result.Hand.IsBusted, Is.False);
            Assert.That(result.Hand.Cards.Count, Is.EqualTo(2));
        }

        [Test]
        public void MapHandInfo_ShouldMapAllProperties()
        {
            // Arrange
            var hand = new Hand();
            hand.AddCard(new Card(Rank.Ten, Suit.Hearts));
            hand.AddCard(new Card(Rank.Nine, Suit.Spades));

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapHandInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (HandInfo)method.Invoke(service, new object[] { hand });

            // Assert
            Assert.That(result.Value, Is.EqualTo(19));
            Assert.That(result.IsBusted, Is.False);
            Assert.That(result.HasBlackjack, Is.False);
            Assert.That(result.Cards.Count, Is.EqualTo(2));

            // Check first card
            Assert.That(result.Cards[0].Rank, Is.EqualTo((int)Rank.Ten));
            Assert.That(result.Cards[0].Suit, Is.EqualTo((int)Suit.Hearts));
            Assert.That(result.Cards[0].IsFaceUp, Is.True);

            // Check second card
            Assert.That(result.Cards[1].Rank, Is.EqualTo((int)Rank.Nine));
            Assert.That(result.Cards[1].Suit, Is.EqualTo((int)Suit.Spades));
            Assert.That(result.Cards[1].IsFaceUp, Is.True);
        }

        [Test]
        public void MapHandInfo_BustedHand_ShouldMapCorrectly()
        {
            // Arrange
            var hand = new Hand();
            hand.AddCard(new Card(Rank.King, Suit.Hearts));
            hand.AddCard(new Card(Rank.Queen, Suit.Spades));
            hand.AddCard(new Card(Rank.Five, Suit.Diamonds));

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapHandInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (HandInfo)method.Invoke(service, new object[] { hand });

            // Assert
            Assert.That(result.Value, Is.EqualTo(25));
            Assert.That(result.IsBusted, Is.True);
            Assert.That(result.HasBlackjack, Is.False);
        }

        [Test]
        public void MapHandInfo_BlackjackHand_ShouldMapCorrectly()
        {
            // Arrange
            var hand = new Hand();
            hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            hand.AddCard(new Card(Rank.King, Suit.Spades));

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapHandInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (HandInfo)method.Invoke(service, new object[] { hand });

            // Assert
            Assert.That(result.Value, Is.EqualTo(21));
            Assert.That(result.IsBusted, Is.False);
            Assert.That(result.HasBlackjack, Is.True);
        }

        [Test]
        public void MapDealerInfo_ShouldMapHandCorrectly()
        {
            // Arrange
            var dealer = new Dealer();
            dealer.Hand.AddCard(new Card(Rank.Seven, Suit.Hearts));
            dealer.Hand.AddCard(new Card(Rank.Ten, Suit.Spades));

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapDealerInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (DealerInfo)method.Invoke(service, new object[] { dealer });

            // Assert
            Assert.That(result.Hand, Is.Not.Null);
            Assert.That(result.Hand.Value, Is.EqualTo(17));
            Assert.That(result.Hand.Cards.Count, Is.EqualTo(2));
        }

        [Test]
        public void CreateErrorResponse_ShouldCreateCorrectResponse()
        {
            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("CreateErrorResponse", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (GameStateResponse)method.Invoke(service, new object[] { "Test error message" });

            // Assert
            Assert.That(result.Success, Is.False);
            Assert.That(result.Message, Is.EqualTo("Test error message"));
        }

        [Test]
        public void CreateGameStateResponse_SinglePlayer_ShouldIncludeCorrectData()
        {
            // Arrange
            testGame.Player1.Name = "TestPlayer";
            testGame.Player1.Id = "player123";
            testGame.Player1.Balance = 1000;
            testGame.Player1.CurrentBet = 100;

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("CreateGameStateResponse", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (GameStateResponse)method.Invoke(service, new object[] { testGame, "player123" });

            // Assert
            Assert.That(result.Success, Is.True);
            Assert.That(result.GamePhase, Is.EqualTo(GameStateResponse.Types.GamePhase.PlacingBets));
            Assert.That(result.Players.Count, Is.EqualTo(1));
            Assert.That(result.Players[0].Name, Is.EqualTo("TestPlayer"));
            Assert.That(result.Players[0].Id, Is.EqualTo("player123"));
            Assert.That(result.Players[0].IsCurrentPlayer, Is.True);
            Assert.That(result.Dealer, Is.Not.Null);
        }

        [Test]
        public void CreateGameStateResponse_TwoPlayer_ShouldIncludeBothPlayers()
        {
            // Arrange
            var twoPlayerGame = new BlackjackGameEngine(true);
            twoPlayerGame.Player1.Name = "Player1";
            twoPlayerGame.Player1.Id = "player1";
            twoPlayerGame.Player2 = new Player("Player2");
            twoPlayerGame.Player2.Id = "player2";

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("CreateGameStateResponse", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (GameStateResponse)method.Invoke(service, new object[] { twoPlayerGame, "player1" });

            // Assert
            Assert.That(result.Players.Count, Is.EqualTo(2));
            Assert.That(result.Players[0].Name, Is.EqualTo("Player1"));
            Assert.That(result.Players[0].IsCurrentPlayer, Is.True);
            Assert.That(result.Players[1].Name, Is.EqualTo("Player2"));
            Assert.That(result.Players[1].IsCurrentPlayer, Is.False);
        }

        [Test]
        public void MapHandInfo_EmptyHand_ShouldMapCorrectly()
        {
            // Arrange
            var hand = new Hand();

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapHandInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (HandInfo)method.Invoke(service, new object[] { hand });

            // Assert
            Assert.That(result.Value, Is.EqualTo(0));
            Assert.That(result.IsBusted, Is.False);
            Assert.That(result.HasBlackjack, Is.False);
            Assert.That(result.Cards.Count, Is.EqualTo(0));
        }

        [Test]
        public void MapHandInfo_WithFaceDownCards_ShouldMapFaceUpState()
        {
            // Arrange
            var hand = new Hand();
            var card1 = new Card(Rank.King, Suit.Hearts);
            var card2 = new Card(Rank.Ace, Suit.Spades);

            card1.IsFaceUp = true;
            card2.IsFaceUp = false; // Face down card

            hand.AddCard(card1);
            hand.AddCard(card2);

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapHandInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (HandInfo)method.Invoke(service, new object[] { hand });

            // Assert
            Assert.That(result.Cards[0].IsFaceUp, Is.True);
            Assert.That(result.Cards[1].IsFaceUp, Is.False);
        }

        [Test]
        public void MapPlayerInfo_NotCurrentPlayer_ShouldSetCorrectly()
        {
            // Arrange
            var player = new Player("TestPlayer");
            player.Id = "player123";

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapPlayerInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (PlayerInfo)method.Invoke(service, new object[] { player, false });

            // Assert
            Assert.That(result.IsCurrentPlayer, Is.False);
        }

        [Test]
        public void CreateGameStateResponse_ShouldHandleNullPlayer2()
        {
            // Arrange
            var singlePlayerGame = new BlackjackGameEngine(false);
            singlePlayerGame.Player1.Name = "Player1";
            singlePlayerGame.Player1.Id = "player1";

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("CreateGameStateResponse", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (GameStateResponse)method.Invoke(service, new object[] { singlePlayerGame, "player1" });

            // Assert
            Assert.That(result.Players.Count, Is.EqualTo(1), "Should only include Player1 when Player2 is null");
        }

        [Test]
        public void CreateGameStateResponse_TwoPlayerModeWithNullPlayer2_ShouldOnlyIncludePlayer1()
        {
            // Arrange
            var twoPlayerGame = new BlackjackGameEngine(true);
            twoPlayerGame.Player1.Name = "Player1";
            twoPlayerGame.Player1.Id = "player1";
            // Player2 remains null

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("CreateGameStateResponse", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (GameStateResponse)method.Invoke(service, new object[] { twoPlayerGame, "player1" });

            // Assert
            Assert.That(result.Players.Count, Is.EqualTo(1), "Should only include Player1 when Player2 is null in two-player mode");
        }

        [Test]
        public void MapAllCardRanksAndSuits_ShouldMapCorrectly()
        {
            // Arrange
            var hand = new Hand();

            // Add cards with all different ranks
            hand.AddCard(new Card(Rank.Two, Suit.Hearts));
            hand.AddCard(new Card(Rank.Jack, Suit.Diamonds));
            hand.AddCard(new Card(Rank.Ace, Suit.Clubs));
            hand.AddCard(new Card(Rank.King, Suit.Spades));

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapHandInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (HandInfo)method.Invoke(service, new object[] { hand });

            // Assert
            Assert.That(result.Cards[0].Rank, Is.EqualTo((int)Rank.Two));
            Assert.That(result.Cards[0].Suit, Is.EqualTo((int)Suit.Hearts));

            Assert.That(result.Cards[1].Rank, Is.EqualTo((int)Rank.Jack));
            Assert.That(result.Cards[1].Suit, Is.EqualTo((int)Suit.Diamonds));

            Assert.That(result.Cards[2].Rank, Is.EqualTo((int)Rank.Ace));
            Assert.That(result.Cards[2].Suit, Is.EqualTo((int)Suit.Clubs));

            Assert.That(result.Cards[3].Rank, Is.EqualTo((int)Rank.King));
            Assert.That(result.Cards[3].Suit, Is.EqualTo((int)Suit.Spades));
        }

        [Test]
        public void GetStateMessage_WithNullCurrentPlayer_ShouldNotThrow()
        {
            // Arrange
            testGame.CurrentPlayer = null;
            SetGameState(testGame, GameState.PlayerTurn);

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("GetStateMessage", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act & Assert
            Assert.DoesNotThrow(() => method.Invoke(service, new object[] { testGame }));
        }

        [Test]
        public void CreateGameStateResponse_DifferentGameStates_ShouldMapCorrectly()
        {
            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("CreateGameStateResponse", BindingFlags.NonPublic | BindingFlags.Instance);

            // Test PlayerTurn state
            SetGameState(testGame, GameState.PlayerTurn);
            var playerTurnResult = (GameStateResponse)method.Invoke(service, new object[] { testGame, "player1" });
            Assert.That(playerTurnResult.GamePhase, Is.EqualTo(GameStateResponse.Types.GamePhase.PlayerTurn));

            // Test DealerTurn state
            SetGameState(testGame, GameState.DealerTurn);
            var dealerTurnResult = (GameStateResponse)method.Invoke(service, new object[] { testGame, "player1" });
            Assert.That(dealerTurnResult.GamePhase, Is.EqualTo(GameStateResponse.Types.GamePhase.DealerTurn));

            // Test GameOver state
            SetGameState(testGame, GameState.GameOver);
            var gameOverResult = (GameStateResponse)method.Invoke(service, new object[] { testGame, "player1" });
            Assert.That(gameOverResult.GamePhase, Is.EqualTo(GameStateResponse.Types.GamePhase.GameOver));
        }

        [Test]
        public void MapPlayerInfo_WithComplexHand_ShouldMapCorrectly()
        {
            // Arrange
            var player = new Player("ComplexPlayer");
            player.Id = "complex123";
            player.Balance = 2500;
            player.CurrentBet = 250;

            // Add multiple cards for complex scenario
            player.Hand.AddCard(new Card(Rank.Ace, Suit.Hearts));
            player.Hand.AddCard(new Card(Rank.Six, Suit.Spades));
            player.Hand.AddCard(new Card(Rank.Four, Suit.Diamonds));
            // This should be 21 (11 + 6 + 4, or 1 + 6 + 4 = 11, but ace optimizes to 11)

            // Use reflection to test the private method
            var method = typeof(BlackjackServiceImpl).GetMethod("MapPlayerInfo", BindingFlags.NonPublic | BindingFlags.Instance);

            // Act
            var result = (PlayerInfo)method.Invoke(service, new object[] { player, false });

            // Assert
            Assert.That(result.Name, Is.EqualTo("ComplexPlayer"));
            Assert.That(result.Balance, Is.EqualTo(2500));
            Assert.That(result.CurrentBet, Is.EqualTo(250));
            Assert.That(result.Hand.Value, Is.EqualTo(21));
            Assert.That(result.Hand.HasBlackjack, Is.False, "Three cards cannot be blackjack");
            Assert.That(result.Hand.Cards.Count, Is.EqualTo(3));
        }

        // Helper method to set game state using reflection
        private void SetGameState(BlackjackGameEngine game, GameState state)
        {
            var stateField = typeof(BlackjackGameEngine).GetField("<State>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
            if (stateField != null)
            {
                stateField.SetValue(game, state);
            }
            else
            {
                // Alternative approach if field name is different
                var stateProperty = typeof(BlackjackGameEngine).GetProperty("State");
                var setter = stateProperty.GetSetMethod(true);
                setter?.Invoke(game, new object[] { state });
            }
        }
    }
}