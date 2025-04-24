
// BlackjackGame.Server/Services/GameManager.cs
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Dynamic;
using BlackjackGame.Core.Game;
using BlackjackGame.Core.Models;
using Grpc.Core;
using Microsoft.AspNetCore.Identity.Data;

namespace BlackjackGame.Server.Services
{
    public class GameManager
    {
        private readonly ConcurrentDictionary<string, BlackjackGame> games = new ConcurrentDictionary<string, BlackjackGame>();
        private readonly ConcurrentDictionary<string, string> playerGameMapping = new ConcurrentDictionary<string, string>();
        private readonly object gameLock = new object();

        public string CreateGame(bool isTwoPlayerMode)
        {
            string gameId = Guid.NewGuid().ToString();
            var game = new BlackjackGame(isTwoPlayerMode);
            games[gameId] = game;
            return gameId;
        }

        public string JoinGame(string playerName)
        {
            lock (gameLock)
            {
                string playerId = Guid.NewGuid().ToString();

                // Suche nach vorhandenen Spielen mit einem offenen Platz
                foreach (var gameEntry in games)
                {
                    var game = gameEntry.Value;
                    if (game.IsTwoPlayerMode && game.Player2 == null)
                    {
                        // Füge Spieler dem vorhandenen Spiel hinzu
                        game.Player2 = new Player(playerName);
                        playerGameMapping[playerId] = gameEntry.Key;
                        return playerId;
                    }
                }

                // Erstelle ein neues Spiel
                string gameId = CreateGame(true);
                var newGame = games[gameId];
                newGame.Player1.Name = playerName;
                playerGameMapping[playerId] = gameId;
                return playerId;
            }
        }

        public BlackjackGame GetGameForPlayer(string playerId)
        {
            if (playerGameMapping.TryGetValue(playerId, out string gameId))
            {
                if (games.TryGetValue(gameId, out BlackjackGame game))
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

            if (game.Player1.Name == playerId)
                return game.Player1;
            else if (game.IsTwoPlayerMode && game.Player2?.Name == playerId)
                return game.Player2;

            return null;
        }

        public void RemovePlayer(string playerId)
        {
            if (playerGameMapping.TryRemove(playerId, out string gameId))
            {
                if (games.TryGetValue(gameId, out BlackjackGame game))
                {
                    // Wenn kein Spieler mehr im Spiel ist, entferne das Spiel
                    bool removeGame = false;

                    if (game.Player1.Name == playerId)
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
                    else if (game.IsTwoPlayerMode && game.Player2?.Name == playerId)
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
