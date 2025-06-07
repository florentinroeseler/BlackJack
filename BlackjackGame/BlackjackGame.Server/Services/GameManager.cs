using System;
using System.Collections.Concurrent;
using BlackjackGame.Core.Game;
using BlackjackGame.Core.Models;

namespace BlackjackGame.Server.Services
{
    public class GameManager
    {
        private readonly ConcurrentDictionary<string, BlackjackGameEngine> games = new ConcurrentDictionary<string, BlackjackGameEngine>();
        private readonly ConcurrentDictionary<string, string> playerGameMapping = new ConcurrentDictionary<string, string>();
        private readonly object gameLock = new object();

        public GameManager()
        {
            Console.WriteLine("GameManager initialisiert");
        }

        private void Debug(string message)
        {
            Console.WriteLine($"MANAGER: {message}");
        }

        public string CreateGame(bool isTwoPlayerMode)
        {
            string gameId = Guid.NewGuid().ToString();
            var game = new BlackjackGameEngine(isTwoPlayerMode);
            games[gameId] = game;
            Debug($"Neues Spiel erstellt: ID={gameId}, TwoPlayerMode={isTwoPlayerMode}");
            return gameId;
        }

        public string JoinGame(string playerName)
        {
            lock (gameLock)
            {
                string playerId = Guid.NewGuid().ToString();
                Debug($"Spieler versucht beizutreten: Name={playerName}, ID={playerId}");

                foreach (var gameEntry in games)
                {
                    var game = gameEntry.Value;
                    Debug($"Prüfe Spiel: {gameEntry.Key}, IsTwoPlayerMode={game.IsTwoPlayerMode}, Player2 existiert: {game.Player2 != null}");

                    if (game.IsTwoPlayerMode && (game.Player2 == null || string.IsNullOrEmpty(game.Player2.Id)))
                    {
                        if (game.Player2 == null)
                        {
                            game.Player2 = new Player(playerName);
                        }
                        else
                        {
                            game.Player2.Name = playerName;
                        }

                        Debug($"Player2 vor ID-Zuweisung: {game.Player2.Id ?? "null"}");
                        game.Player2.Id = playerId;
                        Debug($"Player2 nach ID-Zuweisung: {game.Player2.Id ?? "null"}");

                        playerGameMapping[playerId] = gameEntry.Key;

                        Debug($"Spieler 2 dem Spiel {gameEntry.Key} hinzugefügt: ID={playerId}, Name={playerName}");
                        Debug($"Player1: ID={game.Player1?.Id ?? "null"}, Name={game.Player1?.Name ?? "null"}");
                        Debug($"Player2: ID={game.Player2?.Id ?? "null"}, Name={game.Player2?.Name ?? "null"}");
                        Debug($"CurrentPlayer: {game.CurrentPlayer?.Name ?? "null"}");

                        return playerId;
                    }
                }

                string gameId = CreateGame(true);
                var newGame = games[gameId];

                newGame.Player1.Name = playerName;
                newGame.Player1.Id = playerId;
                playerGameMapping[playerId] = gameId;

                Debug($"Neues Spiel mit Spieler 1 erstellt: GameID={gameId}, PlayerID={playerId}, Name={playerName}");
                Debug($"Player1: ID={newGame.Player1?.Id ?? "null"}, Name={newGame.Player1?.Name ?? "null"}");

                return playerId;
            }
        }

        public BlackjackGameEngine GetGameForPlayer(string playerId)
        {
            if (playerGameMapping.TryGetValue(playerId, out string gameId))
            {
                if (games.TryGetValue(gameId, out BlackjackGameEngine game))
                {
                    return game;
                }
            }
            return null;
        }

        public Player GetPlayerById(string playerId)
        {
            var game = GetGameForPlayer(playerId);
            if (game == null) return null;

            if (game.Player1.Id == playerId)
                return game.Player1;
            else if (game.IsTwoPlayerMode && game.Player2?.Id == playerId)
                return game.Player2;

            return null;
        }

        public void RemovePlayer(string playerId)
        {
            if (playerGameMapping.TryRemove(playerId, out string gameId))
            {
                if (games.TryGetValue(gameId, out BlackjackGameEngine game))
                {
                    bool removeGame = false;

                    if (game.Player1.Id == playerId)
                    {
                        if (!game.IsTwoPlayerMode || game.Player2 == null)
                            removeGame = true;
                        else
                        {
                            game.Player1 = game.Player2;
                            game.Player2 = null;
                        }
                    }
                    else if (game.IsTwoPlayerMode && game.Player2?.Id == playerId)
                    {
                        game.Player2 = null;
                    }

                    if (removeGame)
                    {
                        games.TryRemove(gameId, out _);
                    }
                }
            }
        }
    }
}