// BlackjackGame.Server/Services/BlackjackServiceImpl.cs
using System;
using System.Threading.Tasks;
using Grpc.Core;
using BlackjackGame.Core.Game;
using BlackjackGame.Core.Models;
using BlackjackGame.Core.Protos;

namespace BlackjackGame.Server.Services
{
    public class BlackjackServiceImpl : BlackjackService.BlackjackServiceBase
    {
        private readonly GameManager _gameManager;

        public BlackjackServiceImpl(GameManager gameManager)
        {
            _gameManager = gameManager;
            Console.WriteLine("BlackjackServiceImpl initialisiert");
        }

        private void Debug(string message)
        {
            Console.WriteLine($"DEBUG: {message}");
        }

        public override Task<JoinResponse> JoinGame(JoinRequest request, ServerCallContext context)
        {
            Debug($"JoinGame: PlayerName={request.PlayerName}");

            string playerId = _gameManager.JoinGame(request.PlayerName);

            Debug($"Spieler verbunden: ID={playerId}, Name={request.PlayerName}");

            // Prüfe aktuelle Konfiguration
            var game = _gameManager.GetGameForPlayer(playerId);
            if (game != null)
            {
                Debug($"Nach JoinGame: Spiel gefunden für PlayerId {playerId}");
                Debug($"Player1: ID={game.Player1?.Id ?? "null"}, Name={game.Player1?.Name ?? "null"}");
                if (game.Player2 != null)
                {
                    Debug($"Player2: ID={game.Player2?.Id ?? "null"}, Name={game.Player2?.Name ?? "null"}");
                }
                else
                {
                    Debug("Player2 ist null");
                }
                Debug($"CurrentPlayer: {game.CurrentPlayer?.Name ?? "null"}");
            }
            else
            {
                Debug($"FEHLER: Kein Spiel gefunden für PlayerId {playerId} nach JoinGame!");
            }

            return Task.FromResult(new JoinResponse
            {
                Success = true,
                PlayerId = playerId,
                Message = $"Successfully joined the game as {request.PlayerName}"
            });
        }

        public override Task<GameStateResponse> PlaceBet(BetRequest request, ServerCallContext context)
        {
            Debug($"PlaceBet: PlayerId={request.PlayerId}, Amount={request.BetAmount}");

            var game = _gameManager.GetGameForPlayer(request.PlayerId);
            if (game == null)
            {
                Debug("Spiel nicht gefunden!");
                return Task.FromResult(CreateErrorResponse("Game not found"));
            }

            lock (game)
            {
                var p1Id = game.Player1?.Id ?? "null";
                var p1Name = game.Player1?.Name ?? "null";
                var p2Id = game.Player2?.Id ?? "null";
                var p2Name = game.Player2?.Name ?? "null";
                var currentPlayerName = game.CurrentPlayer?.Name ?? "null";

                Debug($"Spielstatus: {game.State}");
                Debug($"Player1: ID={p1Id}, Name={p1Name}");
                Debug($"Player2: ID={p2Id}, Name={p2Name}");
                Debug($"CurrentPlayer: {currentPlayerName}");

                if (game.State != GameState.PlacingBets)
                {
                    Debug($"Falscher Spielstatus: {game.State}");
                    return Task.FromResult(CreateErrorResponse("Cannot place bet at this time"));
                }

                Player currentPlayer = null;

                // Erster Vergleich: Request PlayerId mit Player1.Id
                Debug($"Vergleiche Request.PlayerId={request.PlayerId} mit Player1.Id={p1Id}");
                bool match1 = (game.Player1?.Id == request.PlayerId);
                Debug($"Match mit Player1.Id: {match1}");

                if (match1)
                {
                    currentPlayer = game.Player1;
                    Debug("Spieler 1 identifiziert");
                }
                else if (game.IsTwoPlayerMode && game.Player2 != null)
                {
                    // Zweiter Vergleich: Request PlayerId mit Player2.Id
                    Debug($"Vergleiche Request.PlayerId={request.PlayerId} mit Player2.Id={p2Id}");
                    bool match2 = (game.Player2?.Id == request.PlayerId);
                    Debug($"Match mit Player2.Id: {match2}");

                    if (match2)
                    {
                        currentPlayer = game.Player2;
                        Debug("Spieler 2 identifiziert");
                    }
                }

                if (currentPlayer == null)
                {
                    Debug("Kein passender Spieler gefunden");
                    return Task.FromResult(CreateErrorResponse("Player not found"));
                }

                bool isCurrentPlayer = (currentPlayer == game.CurrentPlayer);
                Debug($"Ist aktueller Spieler: {isCurrentPlayer}");

                if (!isCurrentPlayer)
                {
                    Debug($"Nicht am Zug. CurrentPlayer={game.CurrentPlayer?.Name}, RequestPlayer={currentPlayer.Name}");
                    return Task.FromResult(CreateErrorResponse("Not your turn"));
                }

                Debug($"Einsatz platziert: {request.BetAmount} für Spieler {currentPlayer.Name}");

                game.PlaceBet(request.BetAmount);

                return Task.FromResult(CreateGameStateResponse(game, request.PlayerId));
            }
        }

        public override Task<GameStateResponse> Hit(ActionRequest request, ServerCallContext context)
        {
            var game = _gameManager.GetGameForPlayer(request.PlayerId);
            if (game == null)
            {
                return Task.FromResult(CreateErrorResponse("Game not found"));
            }

            lock (game)
            {
                if (game.State != GameState.PlayerTurn)
                {
                    return Task.FromResult(CreateErrorResponse("Cannot hit at this time"));
                }

                Player currentPlayer = null;
                if (game.Player1.Id == request.PlayerId) // Geändert: Vergleiche mit Id statt Name
                {
                    currentPlayer = game.Player1;
                }
                else if (game.IsTwoPlayerMode && game.Player2?.Id == request.PlayerId) // Geändert: Vergleiche mit Id statt Name
                {
                    currentPlayer = game.Player2;
                }

                if (currentPlayer == null || currentPlayer != game.CurrentPlayer)
                {
                    return Task.FromResult(CreateErrorResponse("Not your turn"));
                }

                game.Hit();

                return Task.FromResult(CreateGameStateResponse(game, request.PlayerId));
            }
        }

        public override Task<GameStateResponse> Stand(ActionRequest request, ServerCallContext context)
        {
            var game = _gameManager.GetGameForPlayer(request.PlayerId);
            if (game == null)
            {
                return Task.FromResult(CreateErrorResponse("Game not found"));
            }

            lock (game)
            {
                if (game.State != GameState.PlayerTurn)
                {
                    return Task.FromResult(CreateErrorResponse("Cannot stand at this time"));
                }

                Player currentPlayer = null;
                if (game.Player1.Id == request.PlayerId) // Geändert: Vergleiche mit Id statt Name
                {
                    currentPlayer = game.Player1;
                }
                else if (game.IsTwoPlayerMode && game.Player2?.Id == request.PlayerId) // Geändert: Vergleiche mit Id statt Name
                {
                    currentPlayer = game.Player2;
                }

                if (currentPlayer == null || currentPlayer != game.CurrentPlayer)
                {
                    return Task.FromResult(CreateErrorResponse("Not your turn"));
                }

                game.Stand();

                return Task.FromResult(CreateGameStateResponse(game, request.PlayerId));
            }
        }

        public override Task<GameStateResponse> GetGameState(GameStateRequest request, ServerCallContext context)
        {
            var game = _gameManager.GetGameForPlayer(request.PlayerId);
            if (game == null)
            {
                return Task.FromResult(CreateErrorResponse("Game not found"));
            }

            lock (game)
            {
                return Task.FromResult(CreateGameStateResponse(game, request.PlayerId));
            }
        }

        public override async Task<GameStateResponse> StartNewRound(NewRoundRequest request, ServerCallContext context)
        {
            var game = _gameManager.GetGameForPlayer(request.PlayerId);
            if (game == null)
            {
                return CreateErrorResponse("Game not found");
            }

            lock (game)
            {
                if (game.State != GameState.GameOver)
                {
                    return CreateErrorResponse("Cannot start a new round now");
                }

                // Prüfen, ob der Spieler noch genug Geld hat
                if (game.Player1.Balance <= 0 || (game.IsTwoPlayerMode && game.Player2?.Balance <= 0))
                {
                    return CreateErrorResponse("Game over - insufficient balance");
                }

                // Neue Runde starten
                game.StartNewRound();

                return CreateGameStateResponse(game, request.PlayerId);
            }
        }

        private GameStateResponse CreateGameStateResponse(BlackjackGameEngine game, string playerId)
        {
            var response = new GameStateResponse
            {
                Success = true,
                GamePhase = MapGameState(game.State),
                Dealer = MapDealerInfo(game.Dealer),
                Message = GetStateMessage(game)
            };

            // Spieler hinzufügen
            response.Players.Add(MapPlayerInfo(game.Player1, game.CurrentPlayer == game.Player1));

            if (game.IsTwoPlayerMode && game.Player2 != null)
            {
                response.Players.Add(MapPlayerInfo(game.Player2, game.CurrentPlayer == game.Player2));
            }

            return response;
        }

        private string GetStateMessage(BlackjackGameEngine game)
        {
            switch (game.State)
            {
                case GameState.PlacingBets:
                    return "Place your bets";
                case GameState.PlayerTurn:
                    return $"{game.CurrentPlayer.Name}'s turn";
                case GameState.DealerTurn:
                    return "Dealer's turn";
                case GameState.GameOver:
                    return "Round over";
                default:
                    return string.Empty;
            }
        }

        private GameStateResponse.Types.GamePhase MapGameState(GameState state)
        {
            switch (state)
            {
                case GameState.PlacingBets:
                    return GameStateResponse.Types.GamePhase.PlacingBets;
                case GameState.PlayerTurn:
                    return GameStateResponse.Types.GamePhase.PlayerTurn;
                case GameState.DealerTurn:
                    return GameStateResponse.Types.GamePhase.DealerTurn;
                case GameState.GameOver:
                    return GameStateResponse.Types.GamePhase.GameOver;
                default:
                    throw new ArgumentOutOfRangeException(nameof(state));
            }
        }

        private PlayerInfo MapPlayerInfo(Player player, bool isCurrentPlayer)
        {
            var playerInfo = new PlayerInfo
            {
                Name = player.Name, // Hier den Namen des Spielers anzeigen
                Id = player.Id,     // Geändert: Verwende die Id anstelle des Namens
                Balance = player.Balance,
                CurrentBet = player.CurrentBet,
                IsCurrentPlayer = isCurrentPlayer,
                Hand = MapHandInfo(player.Hand)
            };

            return playerInfo;
        }

        private HandInfo MapHandInfo(Hand hand)
        {
            var handInfo = new HandInfo
            {
                Value = hand.GetValue(),
                IsBusted = hand.IsBusted,
                HasBlackjack = hand.HasBlackjack
            };

            foreach (var card in hand.Cards)
            {
                handInfo.Cards.Add(new CardInfo
                {
                    Rank = (int)card.Rank,
                    Suit = (int)card.Suit,
                    IsFaceUp = card.IsFaceUp
                });
            }

            return handInfo;
        }

        private DealerInfo MapDealerInfo(Dealer dealer)
        {
            return new DealerInfo
            {
                Hand = MapHandInfo(dealer.Hand)
            };
        }

        private GameStateResponse CreateErrorResponse(string errorMessage)
        {
            return new GameStateResponse
            {
                Success = false,
                Message = errorMessage
            };
        }
    }
}