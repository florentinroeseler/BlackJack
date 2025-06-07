using NUnit.Framework;
using BlackjackGame.Server.Services;
using BlackjackGame.Core.Game;
using BlackjackGame.Core.Models;
using System;
using System.Threading.Tasks;
using System.Linq;

namespace BlackjackGame.Tests.Services
{
    [TestFixture]
    public class GameManagerTests
    {
        private GameManager gameManager;

        [SetUp]
        public void SetUp()
        {
            gameManager = new GameManager();
        }

        [Test]
        public void CreateGame_SinglePlayerMode_ShouldCreateGame()
        {
            // Act
            var gameId = gameManager.CreateGame(false);

            // Assert
            Assert.That(gameId, Is.Not.Null);
            Assert.That(gameId, Is.Not.Empty);
            Assert.That(Guid.TryParse(gameId, out _), Is.True, "GameId should be a valid GUID");
        }

        [Test]
        public void CreateGame_TwoPlayerMode_ShouldCreateGame()
        {
            // Act
            var gameId = gameManager.CreateGame(true);

            // Assert
            Assert.That(gameId, Is.Not.Null);
            Assert.That(gameId, Is.Not.Empty);
            Assert.That(Guid.TryParse(gameId, out _), Is.True, "GameId should be a valid GUID");
        }

        [Test]
        public void CreateGame_MultipleCalls_ShouldReturnDifferentIds()
        {
            // Act
            var gameId1 = gameManager.CreateGame(false);
            var gameId2 = gameManager.CreateGame(false);

            // Assert
            Assert.That(gameId1, Is.Not.EqualTo(gameId2));
        }

        [Test]
        public void JoinGame_SecondPlayer_ShouldJoinExistingGame()
        {
            // Arrange
            var player1Id = gameManager.JoinGame("Player1");
            var game = gameManager.GetGameForPlayer(player1Id);
            var gameReference = game; // Store reference to check it's the same game

            // Act
            var player2Id = gameManager.JoinGame("Player2");

            // Assert
            Assert.That(player2Id, Is.Not.Null);
            Assert.That(player2Id, Is.Not.EqualTo(player1Id));

            var game1 = gameManager.GetGameForPlayer(player1Id);
            var game2 = gameManager.GetGameForPlayer(player2Id);

            Assert.That(game1, Is.SameAs(game2), "Both players should be in the same game");
            Assert.That(game1, Is.SameAs(gameReference), "Should be the same game instance");

            Assert.That(game1.Player1.Name, Is.EqualTo("Player1"));
            Assert.That(game1.Player1.Id, Is.EqualTo(player1Id));
            Assert.That(game1.Player2, Is.Not.Null);
            Assert.That(game1.Player2.Name, Is.EqualTo("Player2"));
            Assert.That(game1.Player2.Id, Is.EqualTo(player2Id));
        }

        [Test]
        public void GetGameForPlayer_ValidPlayerId_ShouldReturnGame()
        {
            // Arrange
            var playerId = gameManager.JoinGame("TestPlayer");

            // Act
            var game = gameManager.GetGameForPlayer(playerId);

            // Assert
            Assert.That(game, Is.Not.Null);
            Assert.That(game.Player1.Id, Is.EqualTo(playerId));
        }

        [Test]
        public void GetGameForPlayer_InvalidPlayerId_ShouldReturnNull()
        {
            // Act
            var game = gameManager.GetGameForPlayer("invalid-id");

            // Assert
            Assert.That(game, Is.Null);
        }

        [Test]
        public void GetGameForPlayer_EmptyPlayerId_ShouldReturnNull()
        {
            // Act
            var game = gameManager.GetGameForPlayer("");

            // Assert
            Assert.That(game, Is.Null);
        }

        [Test]
        public void GetPlayerById_ValidPlayer1Id_ShouldReturnPlayer()
        {
            // Arrange
            var playerId = gameManager.JoinGame("TestPlayer");

            // Act
            var player = gameManager.GetPlayerById(playerId);

            // Assert
            Assert.That(player, Is.Not.Null);
            Assert.That(player.Id, Is.EqualTo(playerId));
            Assert.That(player.Name, Is.EqualTo("TestPlayer"));
        }

        [Test]
        public void GetPlayerById_ValidPlayer2Id_ShouldReturnPlayer()
        {
            // Arrange
            var player1Id = gameManager.JoinGame("Player1");
            var player2Id = gameManager.JoinGame("Player2");

            // Act
            var player = gameManager.GetPlayerById(player2Id);

            // Assert
            Assert.That(player, Is.Not.Null);
            Assert.That(player.Id, Is.EqualTo(player2Id));
            Assert.That(player.Name, Is.EqualTo("Player2"));
        }

        [Test]
        public void GetPlayerById_InvalidPlayerId_ShouldReturnNull()
        {
            // Act
            var player = gameManager.GetPlayerById("invalid-id");

            // Assert
            Assert.That(player, Is.Null);
        }

        [Test]
        public void RemovePlayer_Player1FromSinglePlayerGame_ShouldRemoveGame()
        {
            // Arrange
            var playerId = gameManager.JoinGame("TestPlayer");
            var game = gameManager.GetGameForPlayer(playerId);
            Assert.That(game, Is.Not.Null);

            // Act
            gameManager.RemovePlayer(playerId);

            // Assert
            var gameAfterRemoval = gameManager.GetGameForPlayer(playerId);
            Assert.That(gameAfterRemoval, Is.Null, "Game should be removed when last player leaves");
        }

        [Test]
        public void RemovePlayer_Player1FromTwoPlayerGame_ShouldPromotePlayer2()
        {
            // Arrange
            var player1Id = gameManager.JoinGame("Player1");
            var player2Id = gameManager.JoinGame("Player2");
            var game = gameManager.GetGameForPlayer(player1Id);

            Assert.That(game.Player1.Name, Is.EqualTo("Player1"));
            Assert.That(game.Player2.Name, Is.EqualTo("Player2"));

            // Act
            gameManager.RemovePlayer(player1Id);

            // Assert
            var gameAfterRemoval = gameManager.GetGameForPlayer(player2Id);
            Assert.That(gameAfterRemoval, Is.Not.Null, "Game should still exist");
            Assert.That(gameAfterRemoval.Player1.Name, Is.EqualTo("Player2"), "Player2 should become Player1");
            Assert.That(gameAfterRemoval.Player1.Id, Is.EqualTo(player2Id));
            Assert.That(gameAfterRemoval.Player2, Is.Null, "Player2 slot should be empty");

            // Player1 should no longer have a game
            var player1Game = gameManager.GetGameForPlayer(player1Id);
            Assert.That(player1Game, Is.Null);
        }

        [Test]
        public void RemovePlayer_Player2FromTwoPlayerGame_ShouldKeepPlayer1()
        {
            // Arrange
            var player1Id = gameManager.JoinGame("Player1");
            var player2Id = gameManager.JoinGame("Player2");

            // Act
            gameManager.RemovePlayer(player2Id);

            // Assert
            var game = gameManager.GetGameForPlayer(player1Id);
            Assert.That(game, Is.Not.Null, "Game should still exist");
            Assert.That(game.Player1.Name, Is.EqualTo("Player1"));
            Assert.That(game.Player1.Id, Is.EqualTo(player1Id));
            Assert.That(game.Player2, Is.Null, "Player2 should be removed");

            // Player2 should no longer have a game
            var player2Game = gameManager.GetGameForPlayer(player2Id);
            Assert.That(player2Game, Is.Null);
        }

        [Test]
        public void RemovePlayer_InvalidPlayerId_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => gameManager.RemovePlayer("invalid-id"));
        }

        [Test]
        public void RemovePlayer_EmptyPlayerId_ShouldNotThrow()
        {
            // Act & Assert
            Assert.DoesNotThrow(() => gameManager.RemovePlayer(""));
        }

        [Test]
        public void ConcurrentJoinGame_ShouldHandleMultiplePlayersCorrectly()
        {
            // Arrange
            var tasks = new Task<string>[10];

            // Act - Multiple players joining simultaneously
            for (int i = 0; i < 10; i++)
            {
                int playerIndex = i;
                tasks[i] = Task.Run(() => gameManager.JoinGame($"Player{playerIndex}"));
            }

            Task.WaitAll(tasks);
            var playerIds = tasks.Select(t => t.Result).ToArray();

            // Assert
            Assert.That(playerIds.Length, Is.EqualTo(10));
            Assert.That(playerIds.Distinct().Count(), Is.EqualTo(10), "All player IDs should be unique");

            // Check that players are distributed correctly in games
            var games = playerIds.Select(id => gameManager.GetGameForPlayer(id)).Distinct().ToArray();
            Assert.That(games.Length, Is.EqualTo(5), "Should create 5 games for 10 players");

            foreach (var game in games)
            {
                Assert.That(game.IsTwoPlayerMode, Is.True);
                Assert.That(game.Player1, Is.Not.Null);
                Assert.That(game.Player2, Is.Not.Null);
            }
        }

        [Test]
        public void JoinGame_WithSpecialCharacters_ShouldWork()
        {
            // Act
            var playerId1 = gameManager.JoinGame("Player@#$%");
            var playerId2 = gameManager.JoinGame("Spieler ÄÖÜ");
            var playerId3 = gameManager.JoinGame("Player123!");

            // Assert
            Assert.That(playerId1, Is.Not.Null);
            Assert.That(playerId2, Is.Not.Null);
            Assert.That(playerId3, Is.Not.Null);

            var player1 = gameManager.GetPlayerById(playerId1);
            var player2 = gameManager.GetPlayerById(playerId2);
            var player3 = gameManager.GetPlayerById(playerId3);

            Assert.That(player1.Name, Is.EqualTo("Player@#$%"));
            Assert.That(player2.Name, Is.EqualTo("Spieler ÄÖÜ"));
            Assert.That(player3.Name, Is.EqualTo("Player123!"));
        }

        [Test]
        public void JoinGame_EmptyPlayerName_ShouldWork()
        {
            // Act
            var playerId = gameManager.JoinGame("");

            // Assert
            Assert.That(playerId, Is.Not.Null);
            var player = gameManager.GetPlayerById(playerId);
            Assert.That(player.Name, Is.EqualTo(""));
        }

        [Test]
        public void JoinGame_NullPlayerName_ShouldWork()
        {
            // Act
            var playerId = gameManager.JoinGame(null);

            // Assert
            Assert.That(playerId, Is.Not.Null);
            var player = gameManager.GetPlayerById(playerId);
            Assert.That(player.Name, Is.Null);
        }

        [Test]
        public void ThreadSafety_MultipleOperations_ShouldNotCorruptState()
        {
            // Arrange
            var joinTasks = new Task<string>[20];
            var removeTasks = new Task[10];

            // Act - Concurrent joins
            for (int i = 0; i < 20; i++)
            {
                int index = i;
                joinTasks[i] = Task.Run(() => gameManager.JoinGame($"Player{index}"));
            }

            Task.WaitAll(joinTasks);
            var playerIds = joinTasks.Select(t => t.Result).ToArray();

            // Concurrent removes of first 10 players
            for (int i = 0; i < 10; i++)
            {
                int index = i;
                removeTasks[i] = Task.Run(() => gameManager.RemovePlayer(playerIds[index]));
            }

            Task.WaitAll(removeTasks);

            // Assert - State should be consistent
            int activeGames = 0;
            for (int i = 10; i < 20; i++)
            {
                var game = gameManager.GetGameForPlayer(playerIds[i]);
                if (game != null)
                {
                    activeGames++;
                }
            }

            Assert.That(activeGames, Is.GreaterThan(0), "Some games should still be active");

            // All remaining players should still be able to get their games
            for (int i = 10; i < 20; i++)
            {
                var game = gameManager.GetGameForPlayer(playerIds[i]);
                var player = gameManager.GetPlayerById(playerIds[i]);

                if (game != null)
                {
                    Assert.That(player, Is.Not.Null);
                    Assert.That(player.Name, Is.EqualTo($"Player{i}"));
                }
            }
        }
    }
}