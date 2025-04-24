// BlackjackGame.Client/Services/BlackjackClient.cs
using System;
using System.Threading.Tasks;
using Grpc.Net.Client;
using BlackjackGame.Core.Protos;
using BlackjackGame.Client.Services;
using BlackjackGame.Client.ViewModels;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Dynamic;
using System.Net;
using System.Security.Claims;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Media3D;
using System.Windows;

namespace BlackjackGame.Client.Services
{
    public class BlackjackClient : IDisposable
    {
        private readonly GrpcChannel _channel;
        private readonly BlackjackService.BlackjackServiceClient _client;
        private string _playerId;

        public BlackjackClient(string serverAddress = "https://localhost:5001")
        {
            // SSL/TLS-Validierung für lokale Entwicklung deaktivieren
            var httpHandler = new System.Net.Http.HttpClientHandler
            {
                ServerCertificateCustomValidationCallback =
                    System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator
            };

            _channel = GrpcChannel.ForAddress(serverAddress, new GrpcChannelOptions
            {
                HttpHandler = httpHandler
            });

            _client = new BlackjackService.BlackjackServiceClient(_channel);
        }

        public async Task<bool> JoinGame(string playerName)
        {
            try
            {
                var response = await _client.JoinGameAsync(new JoinRequest { PlayerName = playerName });
                if (response.Success)
                {
                    _playerId = response.PlayerId;
                    return true;
                }
                return false;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error joining game: {ex.Message}");
                return false;
            }
        }

        public async Task<GameStateResponse> PlaceBet(int amount)
        {
            try
            {
                return await _client.PlaceBetAsync(new BetRequest
                {
                    PlayerId = _playerId,
                    BetAmount = amount
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error placing bet: {ex.Message}");
                return null;
            }
        }

        public async Task<GameStateResponse> Hit()
        {
            try
            {
                return await _client.HitAsync(new ActionRequest { PlayerId = _playerId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error hitting: {ex.Message}");
                return null;
            }
        }

        public async Task<GameStateResponse> Stand()
        {
            try
            {
                return await _client.StandAsync(new ActionRequest { PlayerId = _playerId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error standing: {ex.Message}");
                return null;
            }
        }

        public async Task<GameStateResponse> GetGameState()
        {
            try
            {
                return await _client.GetGameStateAsync(new GameStateRequest { PlayerId = _playerId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error getting game state: {ex.Message}");
                return null;
            }
        }

        public async Task<GameStateResponse> StartNewRound()
        {
            try
            {
                return await _client.StartNewRoundAsync(new NewRoundRequest { PlayerId = _playerId });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error starting new round: {ex.Message}");
                return null;
            }
        }

        public void Dispose()
        {
            _channel?.Dispose();
        }
    }
}
