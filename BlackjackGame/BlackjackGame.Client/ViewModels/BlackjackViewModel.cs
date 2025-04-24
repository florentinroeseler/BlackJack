// BlackjackGame.Client/ViewModels/BlackjackViewModel.cs
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Windows.Input;
using System.Windows.Controls;
using BlackjackGame.Client.Services;
using BlackjackGame.Core.Models;
// Die nächste Zeile ist wichtig - sie importiert die generierten gRPC-Klassen
using BlackjackGame.Core.Protos;
using System.Linq;

namespace BlackjackGame.Client.ViewModels
{
    public class BlackjackViewModel : INotifyPropertyChanged
    {
        private readonly BlackjackClient _client;
        private GameStateResponse _gameState;
        private bool _isConnected;
        private bool _isTwoPlayerMode;
        private string _statusMessage;
        private bool _isPlayerTurn;
        private int _betAmount = 10;
        private string _playerName = "Player 1";
        private readonly LocalBlackjackGame _localGame;

        public event PropertyChangedEventHandler PropertyChanged;

        // Commands
        public ICommand JoinGameCommand { get; }
        public ICommand PlaceBetCommand { get; }
        public ICommand HitCommand { get; }
        public ICommand StandCommand { get; }
        public ICommand StartNewRoundCommand { get; }
        public ICommand StartSinglePlayerGameCommand { get; }

        // Properties
        public bool IsConnected
        {
            get => _isConnected;
            set
            {
                _isConnected = value;
                OnPropertyChanged(nameof(IsConnected));
                OnPropertyChanged(nameof(CanJoinGame));
            }
        }

        public bool IsTwoPlayerMode
        {
            get => _isTwoPlayerMode;
            set
            {
                _isTwoPlayerMode = value;
                OnPropertyChanged(nameof(IsTwoPlayerMode));
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                _statusMessage = value;
                OnPropertyChanged(nameof(StatusMessage));
            }
        }

        public bool IsPlayerTurn
        {
            get => _isPlayerTurn;
            set
            {
                _isPlayerTurn = value;
                OnPropertyChanged(nameof(IsPlayerTurn));
            }
        }

        public int BetAmount
        {
            get => _betAmount;
            set
            {
                _betAmount = value;
                OnPropertyChanged(nameof(BetAmount));
            }
        }

        public string PlayerName
        {
            get => _playerName;
            set
            {
                _playerName = value;
                OnPropertyChanged(nameof(PlayerName));
            }
        }

        public bool CanJoinGame => !IsConnected;

        public ObservableCollection<CardViewModel> PlayerCards { get; } = new ObservableCollection<CardViewModel>();
        public ObservableCollection<CardViewModel> DealerCards { get; } = new ObservableCollection<CardViewModel>();
        public ObservableCollection<CardViewModel> Player2Cards { get; } = new ObservableCollection<CardViewModel>();

        public BlackjackViewModel()
        {
            _client = new BlackjackClient();
            _localGame = new LocalBlackjackGame();

            // Initialize commands
            JoinGameCommand = new RelayCommand(async () => await JoinGame(), () => CanJoinGame);
            PlaceBetCommand = new RelayCommand(PlaceBetAction, () =>
                (_gameState?.GamePhase == GameStateResponse.Types.GamePhase.PlacingBets && IsConnected && IsTwoPlayerMode) ||
                (_localGame.GetGameState() == GameState.PlacingBets && IsConnected && !IsTwoPlayerMode));

            HitCommand = new RelayCommand(HitAction, () =>
                (_gameState?.GamePhase == GameStateResponse.Types.GamePhase.PlayerTurn && IsPlayerTurn && IsTwoPlayerMode) ||
                (_localGame.GetGameState() == GameState.PlayerTurn && !IsTwoPlayerMode));

            StandCommand = new RelayCommand(StandAction, () =>
                (_gameState?.GamePhase == GameStateResponse.Types.GamePhase.PlayerTurn && IsPlayerTurn && IsTwoPlayerMode) ||
                (_localGame.GetGameState() == GameState.PlayerTurn && !IsTwoPlayerMode));

            StartNewRoundCommand = new RelayCommand(StartNewRoundAction, () =>
                (_gameState?.GamePhase == GameStateResponse.Types.GamePhase.GameOver && IsTwoPlayerMode) ||
                (_localGame.GetGameState() == GameState.GameOver && !IsTwoPlayerMode));

            StartSinglePlayerGameCommand = new RelayCommand(StartSinglePlayerGame);

            // Event-Handler für lokales Spiel registrieren
            _localGame.GameStateChanged += OnLocalGameStateChanged;
            _localGame.CardDealt += OnLocalCardDealt;

            // Set default state
            StatusMessage = "Welcome to Blackjack! Choose game mode to start.";
        }

        private async Task JoinGame()
        {
            if (await _client.JoinGame(PlayerName))
            {
                IsConnected = true;
                IsTwoPlayerMode = true;
                StatusMessage = "Connected to server. Waiting for game to start...";

                // Start polling for game state
                _ = StartGameStatePoll();
            }
            else
            {
                StatusMessage = "Failed to connect to the server.";
            }
        }

        private async Task StartGameStatePoll()
        {
            while (IsConnected)
            {
                await UpdateGameState();
                await Task.Delay(1000); // Poll every second
            }
        }

        private async Task UpdateGameState()
        {
            var state = await _client.GetGameState();
            if (state != null)
            {
                UpdateGameStateUI(state);
            }
        }

        private void UpdateGameStateUI(GameStateResponse state)
        {
            _gameState = state;

            StatusMessage = state.Message;


            // With this code:
            var currentPlayer = state.Players.FirstOrDefault(p => p.IsCurrentPlayer);
            IsPlayerTurn = currentPlayer?.Id == state.Players[0].Id; // Assume first player is local player

            // Update cards
            UpdateCards(state);

            // Update UI state based on game phase
            OnPropertyChanged(nameof(PlaceBetCommand));
            OnPropertyChanged(nameof(HitCommand));
            OnPropertyChanged(nameof(StandCommand));
            OnPropertyChanged(nameof(StartNewRoundCommand));
        }

        private void UpdateCards(GameStateResponse state)
        {
            // Update dealer cards
            DealerCards.Clear();
            foreach (var card in state.Dealer.Hand.Cards)
            {
                DealerCards.Add(new CardViewModel(card));
            }

            // Update player cards
            PlayerCards.Clear();
            if (state.Players.Count > 0)
            {
                foreach (var card in state.Players[0].Hand.Cards)
                {
                    PlayerCards.Add(new CardViewModel(card));
                }
            }

            // Update player 2 cards if in two player mode
            Player2Cards.Clear();
            if (state.Players.Count > 1)
            {
                foreach (var card in state.Players[1].Hand.Cards)
                {
                    Player2Cards.Add(new CardViewModel(card));
                }
            }
        }

        private async Task PlaceBet()
        {
            var response = await _client.PlaceBet(BetAmount);
            if (response != null)
            {
                UpdateGameStateUI(response);
            }
            else
            {
                StatusMessage = "Failed to place bet";
            }
        }

        private async Task Hit()
        {
            var response = await _client.Hit();
            if (response != null)
            {
                UpdateGameStateUI(response);
            }
            else
            {
                StatusMessage = "Failed to hit";
            }
        }

        private async Task Stand()
        {
            var response = await _client.Stand();
            if (response != null)
            {
                UpdateGameStateUI(response);
            }
            else
            {
                StatusMessage = "Failed to stand";
            }
        }

        private async Task StartNewRound()
        {
            var response = await _client.StartNewRound();
            if (response != null)
            {
                UpdateGameStateUI(response);
            }
            else
            {
                StatusMessage = "Failed to start new round";
            }
        }

        private void UpdateLocalGameUI()
        {
            // Aktualisiere Status-Nachricht basierend auf Spielzustand
            switch (_localGame.GetGameState())
            {
                case GameState.PlacingBets:
                    StatusMessage = "Place your bet to start";
                    break;
                case GameState.PlayerTurn:
                    StatusMessage = "Your turn: Hit or Stand?";
                    break;
                case GameState.DealerTurn:
                    StatusMessage = "Dealer's turn";
                    break;
                case GameState.GameOver:
                    var player = _localGame.GetPlayer();
                    var dealer = _localGame.GetDealer();

                    if (player.Hand.IsBusted)
                    {
                        StatusMessage = "You busted! Dealer wins.";
                    }
                    else if (dealer.Hand.IsBusted)
                    {
                        StatusMessage = "Dealer busted! You win!";
                    }
                    else if (player.Hand.GetValue() > dealer.Hand.GetValue())
                    {
                        StatusMessage = "You win!";
                    }
                    else if (player.Hand.GetValue() < dealer.Hand.GetValue())
                    {
                        StatusMessage = "Dealer wins.";
                    }
                    else
                    {
                        StatusMessage = "Push (tie).";
                    }

                    StatusMessage += $" Your balance: {player.Balance}";
                    break;
            }

            // Aktualisiere Spielerkarten
            UpdatePlayerCards();

            // Aktualisiere Dealer-Karten
            UpdateDealerCards();

            // Aktualisiere UI-State (für Button-Aktivierung)
            IsPlayerTurn = _localGame.GetGameState() == GameState.PlayerTurn;

            // Benachrichtige UI, dass sich Commands ändern können
            OnPropertyChanged(nameof(PlaceBetCommand));
            OnPropertyChanged(nameof(HitCommand));
            OnPropertyChanged(nameof(StandCommand));
            OnPropertyChanged(nameof(StartNewRoundCommand));
        }

        private void UpdatePlayerCards()
        {
            PlayerCards.Clear();
            foreach (var card in _localGame.GetPlayer().Hand.Cards)
            {
                PlayerCards.Add(new CardViewModel(ConvertToCardInfo(card)));
            }
        }

        private void UpdateDealerCards()
        {
            DealerCards.Clear();
            foreach (var card in _localGame.GetDealer().Hand.Cards)
            {
                DealerCards.Add(new CardViewModel(ConvertToCardInfo(card)));
            }
        }

        private CardInfo ConvertToCardInfo(Card card)
        {
            return new CardInfo
            {
                Rank = (int)card.Rank,
                Suit = (int)card.Suit,
                IsFaceUp = card.IsFaceUp
            };
        }

        // Event-Handler für lokales Spiel
        private void OnLocalGameStateChanged(object sender, GameStateChangedEventArgs e)
        {
            UpdateLocalGameUI();
        }

        private void OnLocalCardDealt(object sender, CardDealtEventArgs e)
        {
            // Hier könnte man Animation für Kartenausgabe hinzufügen
            UpdateLocalGameUI();
        }

        // Aktionsmethoden für Commands, die beide Modi unterstützen
        private void PlaceBetAction()
        {
            if (IsTwoPlayerMode)
            {
                // Verwende Netzwerk-Client für Multiplayer
                _ = PlaceBet();
            }
            else
            {
                // Verwende lokales Spiel für Singleplayer
                _localGame.PlaceBet(BetAmount);
            }
        }

        private void HitAction()
        {
            if (IsTwoPlayerMode)
            {
                _ = Hit();
            }
            else
            {
                _localGame.Hit();
            }
        }

        private void StandAction()
        {
            if (IsTwoPlayerMode)
            {
                _ = Stand();
            }
            else
            {
                _localGame.Stand();
            }
        }

        private void StartNewRoundAction()
        {
            if (IsTwoPlayerMode)
            {
                _ = StartNewRound();
            }
            else
            {
                _ = _localGame.StartNewRound();
            }
        }

        private void StartSinglePlayerGame()
        {
            IsConnected = true;
            IsTwoPlayerMode = false;

            // Initialize local game
            _localGame.StartGame();

            // Update UI with local game state
            UpdateLocalGameUI();

            // Notifiziere UI über geänderten Zustand
            OnPropertyChanged(nameof(PlaceBetCommand));
            OnPropertyChanged(nameof(HitCommand));
            OnPropertyChanged(nameof(StandCommand));
            OnPropertyChanged(nameof(StartNewRoundCommand));
        }

        // Fehlende OnPropertyChanged-Methode
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Helper class for card view model
    public class CardViewModel
    {
        public int Rank { get; }
        public int Suit { get; }
        public bool IsFaceUp { get; }

        public string DisplayText => IsFaceUp ? GetRankName() + " of " + GetSuitName() : "Card Back";

        public CardViewModel(CardInfo cardInfo)
        {
            Rank = cardInfo.Rank;
            Suit = cardInfo.Suit;
            IsFaceUp = cardInfo.IsFaceUp;
        }

        private string GetRankName()
        {
            return Rank switch
            {
                2 => "2",
                3 => "3",
                4 => "4",
                5 => "5",
                6 => "6",
                7 => "7",
                8 => "8",
                9 => "9",
                10 => "10",
                11 => "Jack",
                12 => "Queen",
                13 => "King",
                14 => "Ace",
                _ => Rank.ToString()
            };
        }

        private string GetSuitName()
        {
            return Suit switch
            {
                0 => "Hearts",
                1 => "Diamonds",
                2 => "Clubs",
                3 => "Spades",
                _ => Suit.ToString()
            };
        }
    }

    // Simple relay command implementation
    public class RelayCommand : ICommand
    {
        private readonly Action _execute;
        private readonly Func<bool> _canExecute;

        public RelayCommand(Action execute, Func<bool> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter)
        {
            return _canExecute == null || _canExecute();
        }

        public void Execute(object parameter)
        {
            _execute();
        }

        public event EventHandler CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }
    }
}