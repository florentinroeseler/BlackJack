// BlackjackGame.Server/Services/GameManager.cs
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

                // Suche nach existierenden Spielen mit einem freien Platz
                foreach (var gameEntry in games)
                {
                    var game = gameEntry.Value;
                    Debug($"Prüfe Spiel: {gameEntry.Key}, IsTwoPlayerMode={game.IsTwoPlayerMode}, Player2 existiert: {game.Player2 != null}");

                    if (game.IsTwoPlayerMode && (game.Player2 == null || string.IsNullOrEmpty(game.Player2.Id)))
                    {
                        // Wenn Player2 null ist, erstelle ihn
                        if (game.Player2 == null)
                        {
                            game.Player2 = new Player(playerName);
                        }
                        else
                        {
                            // Sonst aktualisiere vorhandenen Player2
                            game.Player2.Name = playerName;
                        }

                        // Setze ID DIREKT und stelle sicher, dass sie korrekt gesetzt ist
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

                // Erstelle ein neues Spiel für den ersten Spieler
                string gameId = CreateGame(true);
                var newGame = games[gameId];

                // Setze Daten für Spieler 1
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

            // Geändert: Vergleiche mit Id statt Name
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
                    // Wenn kein Spieler mehr im Spiel ist, entferne das Spiel
                    bool removeGame = false;

                    // Geändert: Vergleiche mit Id statt Name
                    if (game.Player1.Id == playerId)
                    {
                        if (!game.IsTwoPlayerMode || game.Player2 == null)
                            removeGame = true;
                        else
                        {
                            // Verschiebe Spieler 2 zu Spieler 1
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