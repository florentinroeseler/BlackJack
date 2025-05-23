﻿// BlackjackGame.Client/ViewModels/BlackjackViewModel.cs
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
        // Füge diese Properties zum BlackjackViewModel.cs hinzu

        // Property für den aktuellen Kontostand
        public string PlayerBalance
        {
            get
            {
                if (IsTwoPlayerMode)
                {
                    // Im Mehrspielermodus den Kontostand aus dem GameState holen
                    var localPlayer = _gameState?.Players?.FirstOrDefault(p => p.Id == _client.PlayerId);
                    return localPlayer != null ? $"{localPlayer.Balance} €" : "0 €";
                }
                else
                {
                    // Im Einzelspielermodus den Kontostand aus dem LocalGame holen
                    return $"{_localGame.GetPlayer().Balance} €";
                }
            }
        }

        // Property für den aktuellen Einsatz 
        public string CurrentBetDisplay
        {
            get
            {
                if (IsTwoPlayerMode)
                {
                    // Im Mehrspielermodus den Einsatz aus dem GameState holen
                    var localPlayer = _gameState?.Players?.FirstOrDefault(p => p.Id == _client.PlayerId);
                    return localPlayer != null ? $"{localPlayer.CurrentBet} €" : "0 €";
                }
                else
                {
                    // Im Einzelspielermodus den Einsatz aus dem LocalGame holen
                    return $"{_localGame.GetPlayer().CurrentBet} €";
                }
            }
        }

        // Property für das Rundenergebnis (gewonnen/verloren)
        private string _roundResultInfo = string.Empty;
        public string RoundResultInfo
        {
            get => _roundResultInfo;
            set
            {
                _roundResultInfo = value;
                OnPropertyChanged(nameof(RoundResultInfo));
                OnPropertyChanged(nameof(HasRoundResult));
            }
        }

        // Property für die Farbe des Rundenergebnisses
        private string _roundResultColor = "White";
        public string RoundResultColor
        {
            get => _roundResultColor;
            set
            {
                _roundResultColor = value;
                OnPropertyChanged(nameof(RoundResultColor));
            }
        }

        // Property zur Bestimmung, ob ein Rundenergebnis angezeigt werden soll
        public bool HasRoundResult
        {
            get => !string.IsNullOrEmpty(_roundResultInfo);
        }

        // Methode zum Aktualisieren des Rundenergebnisses
        // Hilfsmethode zum Speichern des letzten Einsatzes
        private int _lastBetAmount;

        // Ergänzung für UpdateRoundResult - Fallback für Kontostandsermittlung
        private void UpdateRoundResult(int oldBalance, int newBalance)
        {
            int difference = newBalance - oldBalance;
            int betAmount = _lastBetAmount > 0 ? _lastBetAmount : Math.Abs(difference);

            if (difference > 0)
            {
                // Bei Gewinn zeigen wir den Gewinn in Höhe des Einsatzes an
                RoundResultInfo = $"Gewonnen: {betAmount} €";
                RoundResultColor = "LimeGreen";
            }
            else if (difference < 0)
            {
                // Bei Verlust zeigen wir den Verlust als negativen Wert an
                RoundResultInfo = $"Verloren: -{betAmount} €";
                RoundResultColor = "Salmon";
            }
            else // difference == 0
            {
                RoundResultInfo = "Unentschieden (Push)";
                RoundResultColor = "Gold";
            }

            // Nach 5 Sekunden automatisch ausblenden
            Task.Delay(5000).ContinueWith(_ =>
            {
                // UI-Thread verwenden, um UI-Elemente zu aktualisieren
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    RoundResultInfo = string.Empty;
                    OnPropertyChanged(nameof(RoundResultInfo));
                    OnPropertyChanged(nameof(HasRoundResult));
                });
            });
        }

        // Ergänze diese Methode zur LocalGameStateChanged-Event-Behandlung

        private int _previousBalance = 0;

        // 3. Ersetze die OnLocalGameStateChanged-Methode
        private void OnLocalGameStateChanged(object sender, GameStateChangedEventArgs e)
        {
            var player = _localGame.GetPlayer();

            if (e.NewState == GameState.GameOver)
            {
                var dealer = _localGame.GetDealer();

                // Ergebnis basierend auf dem tatsächlichen Spielausgang mit _lastBetAmount
                if (player.Hand.IsBusted)
                {
                    // Spieler hat sich überkauft
                    RoundResultInfo = $"Verloren: -{_lastBetAmount} €";
                    RoundResultColor = "Salmon";
                }
                else if (dealer.Hand.IsBusted)
                {
                    // Dealer hat sich überkauft
                    if (player.Hand.HasBlackjack)
                    {
                        // Blackjack zahlt 3:2
                        int blackjackWin = (int)(_lastBetAmount * 1.5);
                        RoundResultInfo = $"Blackjack: {blackjackWin} €";
                    }
                    else
                    {
                        RoundResultInfo = $"Gewonnen: {_lastBetAmount} €";
                    }
                    RoundResultColor = "LimeGreen";
                }
                else if (player.Hand.GetValue() > dealer.Hand.GetValue())
                {
                    // Spieler hat höheren Wert
                    if (player.Hand.HasBlackjack)
                    {
                        // Blackjack zahlt 3:2
                        int blackjackWin = (int)(_lastBetAmount * 1.5);
                        RoundResultInfo = $"Blackjack: {blackjackWin} €";
                    }
                    else
                    {
                        RoundResultInfo = $"Gewonnen: {_lastBetAmount} €";
                    }
                    RoundResultColor = "LimeGreen";
                }
                else if (player.Hand.GetValue() < dealer.Hand.GetValue())
                {
                    // Dealer hat höheren Wert
                    RoundResultInfo = $"Verloren: -{_lastBetAmount} €";
                    RoundResultColor = "Salmon";
                }
                else
                {
                    // Gleichstand
                    RoundResultInfo = "Unentschieden (Push)";
                    RoundResultColor = "Gold";
                }

                // Debugging
                Console.WriteLine($"Einspielermodus - Ergebnis: {RoundResultInfo}, LastBetAmount: {_lastBetAmount}");

                // Aktualisiere den Kontostand für die nächste Runde
                _previousBalance = player.Balance;
            }
            else if (e.NewState == GameState.PlacingBets)
            {
                // Beim Start einer neuen Runde den aktuellen Kontostand speichern
                _previousBalance = player.Balance;

                // Rundenergebnis zurücksetzen
                RoundResultInfo = string.Empty;
                OnPropertyChanged(nameof(RoundResultInfo));
                OnPropertyChanged(nameof(HasRoundResult));
            }

            UpdateLocalGameUI();
        }

        // Ergänze diese Methode zur UpdateGameStateUI-Methode (direkt nach der Verarbeitung des GameState)

        // Neue Statusvariable hinzufügen
        private bool _roundResultProcessed = false;


        // 2. Ersetze die HandleGameStatePhaseChange-Methode durch diese vereinfachte Version
        private void HandleGameStatePhaseChange()
        {
            // Nur für den Mehrspielermodus
            if (IsTwoPlayerMode && _gameState != null)
            {
                var localPlayer = _gameState?.Players?.FirstOrDefault(p => p.Id == _client.PlayerId);

                if (localPlayer != null)
                {
                    Console.WriteLine($"GamePhase: {_gameState.GamePhase}, PreviousBalance: {_previousBalance}, CurrentBalance: {localPlayer.Balance}, LastBet: {_lastBetAmount}");

                    if (_gameState.GamePhase == GameStateResponse.Types.GamePhase.GameOver)
                    {
                        // Nur einmal pro GameOver-Phase das Ergebnis anzeigen
                        if (!_roundResultProcessed)
                        {
                            // Kartenwerte analysieren
                            bool playerBusted = false;
                            bool dealerBusted = false;
                            int playerValue = 0;
                            int dealerValue = 0;

                            if (localPlayer.Hand != null && _gameState.Dealer?.Hand != null)
                            {
                                playerBusted = localPlayer.Hand.Value > 21;
                                dealerBusted = _gameState.Dealer.Hand.Value > 21;
                                playerValue = localPlayer.Hand.Value;
                                dealerValue = _gameState.Dealer.Hand.Value;
                            }

                            // Wir verwenden den gespeicherten Einsatz (_lastBetAmount)
                            Console.WriteLine($"Determining result with bet amount: {_lastBetAmount}");

                            // Ermittlung des Spielergebnisses basierend auf Kartenwerten
                            if (playerBusted)
                            {
                                // Spieler hat sich überkauft - Verloren
                                RoundResultInfo = $"Verloren: -{_lastBetAmount} €";
                                RoundResultColor = "Salmon";
                            }
                            else if (dealerBusted)
                            {
                                // Dealer hat sich überkauft - Gewonnen
                                RoundResultInfo = $"Gewonnen: {_lastBetAmount} €";
                                RoundResultColor = "LimeGreen";
                            }
                            else if (playerValue > dealerValue)
                            {
                                // Spieler hat höheren Wert - Gewonnen
                                RoundResultInfo = $"Gewonnen: {_lastBetAmount} €";
                                RoundResultColor = "LimeGreen";
                            }
                            else if (playerValue < dealerValue)
                            {
                                // Dealer hat höheren Wert - Verloren
                                RoundResultInfo = $"Verloren: -{_lastBetAmount} €";
                                RoundResultColor = "Salmon";
                            }
                            else
                            {
                                // Gleichstand - Unentschieden
                                RoundResultInfo = "Unentschieden (Push)";
                                RoundResultColor = "Gold";
                            }

                            _roundResultProcessed = true; // Markieren als verarbeitet

                            // Debugging-Ausgabe
                            Console.WriteLine($"Ergebnis berechnet: {RoundResultInfo}");
                        }

                        // Kontostand am Ende der Runde aktualisieren
                        _previousBalance = localPlayer.Balance;
                    }
                    else if (_gameState.GamePhase == GameStateResponse.Types.GamePhase.PlacingBets)
                    {
                        // Beim Start einer neuen Runde den Status zurücksetzen
                        _roundResultProcessed = false;

                        // Speichere den Kontostand VOR dem Platzieren des Einsatzes
                        _previousBalance = localPlayer.Balance;

                        // RoundResultInfo zurücksetzen
                        RoundResultInfo = string.Empty;
                        OnPropertyChanged(nameof(RoundResultInfo));
                        OnPropertyChanged(nameof(HasRoundResult));
                    }
                }
            }
        }


        public bool IsStatusVisible
        {
            get => !string.IsNullOrEmpty(_currentPlayerInfo);
        }

        // In BlackjackViewModel.cs - neue Properties hinzufügen

        private string _opponentName = "Player 2";
        public string OpponentName
        {
            get => _opponentName;
            set
            {
                _opponentName = value;
                OnPropertyChanged(nameof(OpponentName));
            }
        }

        // Status-Farbe für visuelle Hinweise
        private string _statusColor = "White";
        public string StatusColor
        {
            get => _statusColor;
            set
            {
                _statusColor = value;
                OnPropertyChanged(nameof(StatusColor));
            }
        }

        // Information über den aktuellen Spieler
        private string _currentPlayerInfo = string.Empty;
        public string CurrentPlayerInfo
        {
            get => _currentPlayerInfo;
            set
            {
                _currentPlayerInfo = value;
                OnPropertyChanged(nameof(CurrentPlayerInfo));
            }
        }

        // Tooltip-Properties für Buttons
        public string PlaceBetTooltip
        {
            get
            {
                if (!IsConnected)
                    return "Du musst zuerst dem Spiel beitreten";
                if (_gameState?.GamePhase != GameStateResponse.Types.GamePhase.PlacingBets)
                    return "Du kannst nur während der Einsatzphase setzen";
                if (IsTwoPlayerMode && !IsPlayerTurn)
                    return "Du bist nicht am Zug";
                return "Setze deinen Einsatz";
            }
        }

        public string HitTooltip
        {
            get
            {
                if (!IsConnected)
                    return "Du musst zuerst dem Spiel beitreten";
                if (IsTwoPlayerMode)
                {
                    if (_gameState?.GamePhase != GameStateResponse.Types.GamePhase.PlayerTurn)
                        return "Du kannst nur während deines Zuges eine Karte nehmen";
                    if (!IsPlayerTurn)
                        return "Du bist nicht am Zug";
                }
                else
                {
                    if (_localGame.GetGameState() != GameState.PlayerTurn)
                        return "Du kannst nur während deines Zuges eine Karte nehmen";
                }
                return "Nimm eine weitere Karte";
            }
        }

        public string StandTooltip
        {
            get
            {
                if (!IsConnected)
                    return "Du musst zuerst dem Spiel beitreten";
                if (IsTwoPlayerMode)
                {
                    if (_gameState?.GamePhase != GameStateResponse.Types.GamePhase.PlayerTurn)
                        return "Du kannst nur während deines Zuges stehen bleiben";
                    if (!IsPlayerTurn)
                        return "Du bist nicht am Zug";
                }
                else
                {
                    if (_localGame.GetGameState() != GameState.PlayerTurn)
                        return "Du kannst nur während deines Zuges stehen bleiben";
                }
                return "Keine weiteren Karten nehmen";
            }
        }

        public string StartNewRoundTooltip
        {
            get
            {
                if (!IsConnected)
                    return "Du musst zuerst dem Spiel beitreten";
                if (IsTwoPlayerMode)
                {
                    if (_gameState?.GamePhase != GameStateResponse.Types.GamePhase.GameOver)
                        return "Du kannst erst eine neue Runde starten, wenn die aktuelle beendet ist";
                }
                else
                {
                    if (_localGame.GetGameState() != GameState.GameOver)
                        return "Du kannst erst eine neue Runde starten, wenn die aktuelle beendet ist";
                }
                return "Starte eine neue Runde";
            }
        }
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
            // Generiere einen eindeutigen Spielernamen
            _playerName = "Player " + Guid.NewGuid().ToString().Substring(0, 4);
            _client = new BlackjackClient();
            _localGame = new LocalBlackjackGame();

            // Wichtig: Initialisierung des _previousBalance mit dem korrekten Startwert
            _previousBalance = _localGame.GetPlayer().Balance;

            // _roundResultProcessed explizit auf false setzen
            _roundResultProcessed = false;

            // Im BlackjackViewModel.cs - Konstruktor
            JoinGameCommand = new RelayCommand(async () => await JoinGame(), () => CanJoinGame);

            // Im BlackjackViewModel-Konstruktor
            PlaceBetCommand = new RelayCommand(PlaceBetAction, () =>
                // Zwei-Spieler-Modus: Nur wenn Einsatzphase UND Spieler am Zug
                (_gameState?.GamePhase == GameStateResponse.Types.GamePhase.PlacingBets && IsConnected && IsTwoPlayerMode && IsPlayerTurn) ||
                // Einzel-Spieler-Modus
                (_localGame.GetGameState() == GameState.PlacingBets && IsConnected && !IsTwoPlayerMode));

            HitCommand = new RelayCommand(HitAction, () =>
                // Zwei-Spieler-Modus: Nur wenn Spielphase UND Spieler am Zug
                (_gameState?.GamePhase == GameStateResponse.Types.GamePhase.PlayerTurn && IsPlayerTurn && IsTwoPlayerMode) ||
                // Einzel-Spieler-Modus
                (_localGame.GetGameState() == GameState.PlayerTurn && !IsTwoPlayerMode));

            StandCommand = new RelayCommand(StandAction, () =>
                // Zwei-Spieler-Modus: Nur wenn Spielphase UND Spieler am Zug
                (_gameState?.GamePhase == GameStateResponse.Types.GamePhase.PlayerTurn && IsPlayerTurn && IsTwoPlayerMode) ||
                // Einzel-Spieler-Modus
                (_localGame.GetGameState() == GameState.PlayerTurn && !IsTwoPlayerMode));

            StartNewRoundCommand = new RelayCommand(StartNewRoundAction, () =>
                // Beide Modi: Nur wenn Spielende
                (_gameState?.GamePhase == GameStateResponse.Types.GamePhase.GameOver && IsTwoPlayerMode) ||
                (_localGame.GetGameState() == GameState.GameOver && !IsTwoPlayerMode));

            StartSinglePlayerGameCommand = new RelayCommand(StartSinglePlayerGame, () => !IsConnected);

            // Event-Handler für lokales Spiel registrieren
            _localGame.GameStateChanged += OnLocalGameStateChanged;
            _localGame.CardDealt += OnLocalCardDealt;

            // Set default state
            StatusMessage = "Welcome to Blackjack! Choose game mode to start.";
        }

        private string _playerId; // Neue Variable hinzufügen

        // 5. Ergänze auch die JoinGame-Methode, um die _lastBetAmount zurückzusetzen
        private async Task JoinGame()
        {
            // _lastBetAmount zurücksetzen
            _lastBetAmount = 0;

            if (await _client.JoinGame(PlayerName))
            {
                IsConnected = true;
                IsTwoPlayerMode = true;
                _roundResultProcessed = false; // Status zurücksetzen

                // RoundResultInfo zurücksetzen
                RoundResultInfo = string.Empty;

                // Hole die PlayerId aus dem Client
                _playerId = _client.PlayerId;

                // Initialisiere den Kontostand
                var state = await _client.GetGameState();
                if (state != null && state.Success)
                {
                    var localPlayer = state.Players?.FirstOrDefault(p => p.Id == _client.PlayerId);
                    if (localPlayer != null)
                    {
                        _previousBalance = localPlayer.Balance;
                        Console.WriteLine($"Initial balance set to: {_previousBalance}");
                    }
                }

                StatusMessage = $"Connected to server as {PlayerName}. Waiting for game to start...";

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

        // Und die UpdateGameStateUI-Methode anpassen
        // 2. Dann aktualisiere die UpdateGameStateUI-Methode, um den Gegnernamen zu setzen
        private void UpdateGameStateUI(GameStateResponse state)
        {
            _gameState = state;

            StatusMessage = state.Message;

            // Prüfe, ob die Antwort erfolgreich war
            if (!state.Success)
            {
                StatusColor = "Red"; // Fehler rot anzeigen
                return;
            }

            // Nur weitermachen, wenn alle notwendigen Daten vorhanden sind
            if (state.Dealer != null && state.Players != null && state.Players.Count > 0)
            {
                // Finde den lokalen Spieler anhand der ID
                var localPlayer = state.Players.FirstOrDefault(p => p.Id == _client.PlayerId);
                var currentPlayer = state.Players.FirstOrDefault(p => p.IsCurrentPlayer);

                // Finde den Gegner-Spieler (jeder Spieler außer dem lokalen Spieler)
                var opponent = state.Players.FirstOrDefault(p => p.Id != _client.PlayerId);
                if (opponent != null)
                {
                    OpponentName = opponent.Name; // Setze den Namen des Gegners
                }

                // Spielerzug-Status
                IsPlayerTurn = localPlayer != null && localPlayer.IsCurrentPlayer;

                // Aktueller Spieler Info
                if (currentPlayer != null)
                {
                    if (currentPlayer.Id == _client.PlayerId)
                    {
                        CurrentPlayerInfo = "Du bist am Zug!";
                        StatusColor = "LimeGreen"; // Eigener Zug grün anzeigen
                    }
                    else
                    {
                        CurrentPlayerInfo = $"{currentPlayer.Name} ist am Zug";
                        StatusColor = "White"; // Standard weiß
                    }
                }
                else
                {
                    CurrentPlayerInfo = string.Empty;
                    StatusColor = "White";
                }

                // Update cards
                UpdateCards(state);
            }
            else
            {
                StatusColor = "White";
                CurrentPlayerInfo = string.Empty;
            }

            // Aktualisiere Tool-Tip-Properties
            OnPropertyChanged(nameof(PlaceBetTooltip));
            OnPropertyChanged(nameof(HitTooltip));
            OnPropertyChanged(nameof(StandTooltip));
            OnPropertyChanged(nameof(StartNewRoundTooltip));

            // Update UI state based on game phase
            OnPropertyChanged(nameof(PlaceBetCommand));
            OnPropertyChanged(nameof(HitCommand));
            OnPropertyChanged(nameof(StandCommand));
            OnPropertyChanged(nameof(StartNewRoundCommand));

            // WICHTIG: Hier fehlte der Aufruf von HandleGameStatePhaseChange()
            HandleGameStatePhaseChange();

            OnPropertyChanged(nameof(PlayerBalance));
            OnPropertyChanged(nameof(CurrentBetDisplay));
        }
        private void UpdateCards(GameStateResponse state)
        {
            // Update dealer cards
            DealerCards.Clear();
            foreach (var card in state.Dealer.Hand.Cards)
            {
                DealerCards.Add(new CardViewModel(card));
            }

            // Finde den lokalen Spieler anhand der ID
            var localPlayer = state.Players.FirstOrDefault(p => p.Id == _playerId);

            // Update local player cards
            PlayerCards.Clear();
            if (localPlayer != null)
            {
                foreach (var card in localPlayer.Hand.Cards)
                {
                    PlayerCards.Add(new CardViewModel(card));
                }
            }

            // Update other player cards
            Player2Cards.Clear();
            var otherPlayer = state.Players.FirstOrDefault(p => p.Id != _playerId);
            if (otherPlayer != null)
            {
                foreach (var card in otherPlayer.Hand.Cards)
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
            OnPropertyChanged(nameof(PlayerBalance));
            OnPropertyChanged(nameof(CurrentBetDisplay));
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

        private void OnLocalCardDealt(object sender, CardDealtEventArgs e)
        {
            // Hier könnte man Animation für Kartenausgabe hinzufügen
            UpdateLocalGameUI();
        }

        // Aktionsmethoden für Commands, die beide Modi unterstützen
        // 4. Ersetze die PlaceBetAction-Methode - HIER SPEICHERN WIR DEN EINSATZ
        private void PlaceBetAction()
        {
            // WICHTIG: Speichere den aktuellen Einsatz für spätere Verwendung
            _lastBetAmount = BetAmount;
            Console.WriteLine($"Saved bet amount: {_lastBetAmount}");

            if (IsTwoPlayerMode)
            {
                var localPlayer = _gameState?.Players?.FirstOrDefault(p => p.Id == _client.PlayerId);
                if (localPlayer != null)
                {
                    // Speichere den aktuellen Kontostand VOR dem Platzieren des Einsatzes
                    _previousBalance = localPlayer.Balance;
                    Console.WriteLine($"Pre-bet balance saved: {_previousBalance}");
                }

                // Verwende Netzwerk-Client für Multiplayer
                _ = PlaceBet();
            }
            else
            {
                // Im Einzelspielermodus den Kontostand vor dem Platzieren des Einsatzes speichern
                _previousBalance = _localGame.GetPlayer().Balance;
                Console.WriteLine($"Local pre-bet balance saved: {_previousBalance}");

                // Verwende lokales Spiel für Singleplayer
                _localGame.PlaceBet(BetAmount);
            }

            OnPropertyChanged(nameof(PlayerBalance));
            OnPropertyChanged(nameof(CurrentBetDisplay));
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
            OnPropertyChanged(nameof(PlayerBalance));
            OnPropertyChanged(nameof(CurrentBetDisplay));
        }

        // 6. Ergänze auch die StartSinglePlayerGame-Methode, um _lastBetAmount zurückzusetzen
        private void StartSinglePlayerGame()
        {
            // _lastBetAmount zurücksetzen
            _lastBetAmount = 0;

            IsConnected = true;
            IsTwoPlayerMode = false;

            // RoundResultInfo zurücksetzen
            RoundResultInfo = string.Empty;

            // _previousBalance zurücksetzen und neu initialisieren
            _previousBalance = _localGame.GetPlayer().Balance;

            // Initialize local game
            _localGame.StartGame();

            // Update UI with local game state
            UpdateLocalGameUI();

            // Notifiziere UI über geänderten Zustand
            OnPropertyChanged(nameof(PlaceBetCommand));
            OnPropertyChanged(nameof(HitCommand));
            OnPropertyChanged(nameof(StandCommand));
            OnPropertyChanged(nameof(StartNewRoundCommand));
            OnPropertyChanged(nameof(PlayerBalance));
            OnPropertyChanged(nameof(CurrentBetDisplay));
            OnPropertyChanged(nameof(RoundResultInfo));
            OnPropertyChanged(nameof(HasRoundResult));
        }

        // Fehlende OnPropertyChanged-Methode
        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    // Helper class for card view model
    // Erweiterte CardViewModel-Klasse für Blackjack-Spiel
    public class CardViewModel
    {
        public int Rank { get; }
        public int Suit { get; }
        public bool IsFaceUp { get; }

        // Die folgenden Properties werden für die Texturen verwendet
        public string ImageSource => GetImageSource();
        public string DisplayText => IsFaceUp ? GetRankName() + " of " + GetSuitName() : "Card Back";

        public CardViewModel(CardInfo cardInfo)
        {
            Rank = cardInfo.Rank;
            Suit = cardInfo.Suit;
            IsFaceUp = cardInfo.IsFaceUp;
        }

        private string GetImageSource()
        {
            if (!IsFaceUp)
                return "/Assets/Cards/card_backs/card_back.png";

            string suitFolder = GetSuitFolder();
            string rankName = GetRankFileName();

            return $"/Assets/Cards/{suitFolder}/{rankName}.png";
        }

        private string GetSuitFolder()
        {
            return Suit switch
            {
                0 => "hearts",
                1 => "diamonds",
                2 => "clubs",
                3 => "spades",
                _ => "unknown"
            };
        }

        private string GetRankFileName()
        {
            string suitName = GetSuitFolder();

            return Rank switch
            {
                2 => $"{suitName}_2",
                3 => $"{suitName}_3",
                4 => $"{suitName}_4",
                5 => $"{suitName}_5",
                6 => $"{suitName}_6",
                7 => $"{suitName}_7",
                8 => $"{suitName}_8",
                9 => $"{suitName}_9",
                10 => $"{suitName}_10",
                11 => $"{suitName}_jack",
                12 => $"{suitName}_queen",
                13 => $"{suitName}_king",
                14 => $"{suitName}_ace",
                _ => $"{suitName}_{Rank}"
            };
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