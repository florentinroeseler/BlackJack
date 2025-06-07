using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using BlackjackGame.Core.Models;

namespace BlackjackGame.Client.ViewModels
{
    public class LocalBlackjackGame
    {
        private Deck _deck;
        private Dealer _dealer;
        private Player _player;
        private GameState _gameState;

        // Events
        public event EventHandler<GameStateChangedEventArgs> GameStateChanged;
        public event EventHandler<CardDealtEventArgs> CardDealt;

        // Delegates
        public delegate void GameStateChangedHandler(GameState newState);
        public delegate void CardDealtHandler(Card card, bool isPlayerCard);

        public LocalBlackjackGame()
        {
            InitializeGame();
        }

        private void InitializeGame()
        {
            _deck = new Deck();
            _dealer = new Dealer();
            _player = new Player("Player 1", 1000);
            _gameState = GameState.PlacingBets;
        }

        public void StartGame()
        {
            InitializeGame();
            OnGameStateChanged(new GameStateChangedEventArgs { NewState = _gameState });
        }

        public void PlaceBet(int amount)
        {
            if (_gameState != GameState.PlacingBets)
                return;

            if (amount <= 0 || amount > _player.Balance)
                return;

            _player.PlaceBet(amount);
            DealInitialCards();
            _gameState = GameState.PlayerTurn;
            OnGameStateChanged(new GameStateChangedEventArgs { NewState = _gameState });
        }

        private void DealInitialCards()
        {
            Card card = _deck.DrawCard();
            card.IsFaceUp = true;
            _player.Hand.AddCard(card);
            OnCardDealt(new CardDealtEventArgs { Card = card, IsPlayerCard = true });

            card = _deck.DrawCard();
            card.IsFaceUp = true;
            _dealer.Hand.AddCard(card);
            OnCardDealt(new CardDealtEventArgs { Card = card, IsPlayerCard = false });

            card = _deck.DrawCard();
            card.IsFaceUp = true;
            _player.Hand.AddCard(card);
            OnCardDealt(new CardDealtEventArgs { Card = card, IsPlayerCard = true });

            card = _deck.DrawCard();
            card.IsFaceUp = false;
            _dealer.Hand.AddCard(card);
            OnCardDealt(new CardDealtEventArgs { Card = card, IsPlayerCard = false });

            // Check for blackjack
            if (_player.Hand.HasBlackjack)
            {
                DealerTurn();
            }
        }

        public void Hit()
        {
            if (_gameState != GameState.PlayerTurn)
                return;

            Card card = _deck.DrawCard();
            card.IsFaceUp = true;
            _player.Hand.AddCard(card);
            OnCardDealt(new CardDealtEventArgs { Card = card, IsPlayerCard = true });

            if (_player.Hand.IsBusted)
            {
                DealerTurn();
            }
        }

        public void Stand()
        {
            if (_gameState != GameState.PlayerTurn)
                return;

            DealerTurn();
        }

        private void DealerTurn()
        {
            _gameState = GameState.DealerTurn;
            OnGameStateChanged(new GameStateChangedEventArgs { NewState = _gameState });

            _dealer.RevealHiddenCard();

            foreach (var card in _dealer.Hand.Cards)
            {
                if (card.IsFaceUp)
                {
                    OnCardDealt(new CardDealtEventArgs { Card = card, IsPlayerCard = false });
                }
            }

            while (_dealer.Hand.GetValue() < 17)
            {
                Card card = _deck.DrawCard();
                card.IsFaceUp = true;
                _dealer.Hand.AddCard(card);
                OnCardDealt(new CardDealtEventArgs { Card = card, IsPlayerCard = false });
            }

            DetermineWinner();
        }

        private void DetermineWinner()
        {
            int playerValue = _player.Hand.GetValue();
            int dealerValue = _dealer.Hand.GetValue();
            bool dealerBusted = _dealer.Hand.IsBusted;

            if (_player.Hand.IsBusted)
            {
                _player.Lose();
            }
            else if (dealerBusted)
            {
                if (_player.Hand.HasBlackjack)
                {
                    _player.WinBlackjack();
                }
                else
                {
                    _player.Win();
                }
            }
            else if (playerValue > dealerValue)
            {
                if (_player.Hand.HasBlackjack)
                {
                    _player.WinBlackjack();
                }
                else
                {
                    _player.Win();
                }
            }
            else if (playerValue < dealerValue)
            {
                _player.Lose();
            }
            else
            {
                _player.Push();
            }

            _gameState = GameState.GameOver;
            OnGameStateChanged(new GameStateChangedEventArgs { NewState = _gameState });
        }

        public async Task StartNewRound()
        {
            _player.Hand.Clear();
            _dealer.Hand.Clear();

            _gameState = GameState.PlacingBets;
            OnGameStateChanged(new GameStateChangedEventArgs { NewState = _gameState });

            await Task.Delay(500);
        }

        // Getters for game state
        public GameState GetGameState() => _gameState;
        public Dealer GetDealer() => _dealer;
        public Player GetPlayer() => _player;
        public Deck GetDeck() => _deck;

        // Event handlers
        protected virtual void OnGameStateChanged(GameStateChangedEventArgs e)
        {
            GameStateChanged?.Invoke(this, e);
        }

        protected virtual void OnCardDealt(CardDealtEventArgs e)
        {
            CardDealt?.Invoke(this, e);
        }
    }

    // Local game state enum
    public enum GameState
    {
        PlacingBets,
        PlayerTurn,
        DealerTurn,
        GameOver
    }

    // Event args
    public class GameStateChangedEventArgs : EventArgs
    {
        public GameState NewState { get; set; }
    }

    public class CardDealtEventArgs : EventArgs
    {
        public Card Card { get; set; }
        public bool IsPlayerCard { get; set; }
    }
}