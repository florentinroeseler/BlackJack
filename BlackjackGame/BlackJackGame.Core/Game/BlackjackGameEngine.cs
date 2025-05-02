// BlackjackGame.Core/Game/BlackjackGameEngine.cs
using System;
using System.Threading.Tasks;
using BlackjackGame.Core.Models;

namespace BlackjackGame.Core.Game
{
    public enum GameState
    {
        PlacingBets,
        PlayerTurn,
        DealerTurn,
        GameOver
    }

    public class BlackjackGameEngine
    {
        public Deck Deck { get; private set; }
        public Dealer Dealer { get; private set; }
        public Player Player1 { get; set; }  // Öffentlichen Setter hinzugefügt
        public Player Player2 { get; set; }  // Öffentlichen Setter hinzugefügt
        public Player CurrentPlayer { get; private set; }
        public GameState State { get; private set; }
        public bool IsTwoPlayerMode { get; private set; }

        // Events
        public event EventHandler<GameStateChangedEventArgs> GameStateChanged;
        public event EventHandler<PlayerChangedEventArgs> CurrentPlayerChanged;

        // Delegate
        public delegate void CardDealtHandler(Card card, bool isPlayerCard);
        public event CardDealtHandler CardDealt;

        public BlackjackGameEngine(bool twoPlayerMode = false)
        {
            IsTwoPlayerMode = twoPlayerMode;
            Deck = new Deck();
            Dealer = new Dealer();
            Player1 = new Player("Player 1");

            if (twoPlayerMode)
            {
                Player2 = new Player("Player 2");
            }

            State = GameState.PlacingBets;
            CurrentPlayer = Player1;
        }

        public void StartNewRound()
        {
            // Reset hands
            Dealer.Hand.Clear();
            Player1.Hand.Clear();
            if (IsTwoPlayerMode && Player2 != null)
            {
                Player2.Hand.Clear();
            }

            // Reset game state
            State = GameState.PlacingBets;
            OnGameStateChanged(new GameStateChangedEventArgs { NewState = State });

            CurrentPlayer = Player1;
            OnCurrentPlayerChanged(new PlayerChangedEventArgs { NewPlayer = CurrentPlayer });
        }

        public void PlaceBet(int amount)
        {
            if (State != GameState.PlacingBets)
                return;

            CurrentPlayer.PlaceBet(amount);

            // If in two player mode, switch to player 2 for betting
            if (IsTwoPlayerMode && CurrentPlayer == Player1 && Player2 != null)
            {
                CurrentPlayer = Player2;
                OnCurrentPlayerChanged(new PlayerChangedEventArgs { NewPlayer = CurrentPlayer });
                return;
            }

            // Start the game after betting
            DealInitialCards();
            State = GameState.PlayerTurn;
            OnGameStateChanged(new GameStateChangedEventArgs { NewState = State });
        }

        private void DealInitialCards()
        {
            if (IsTwoPlayerMode && Player2 != null)
            {
                Dealer.DealInitialCards(Deck, Player1, Player2);
            }
            else
            {
                Dealer.DealInitialCards(Deck, Player1);
            }

            // Raise events for each card dealt
            foreach (var card in Player1.Hand.Cards)
            {
                CardDealt?.Invoke(card, true);
            }

            if (IsTwoPlayerMode && Player2 != null)
            {
                foreach (var card in Player2.Hand.Cards)
                {
                    CardDealt?.Invoke(card, true);
                }
            }

            foreach (var card in Dealer.Hand.Cards)
            {
                CardDealt?.Invoke(card, false);
            }

            CurrentPlayer = Player1;
            OnCurrentPlayerChanged(new PlayerChangedEventArgs { NewPlayer = CurrentPlayer });
        }

        public void Hit()
        {
            if (State != GameState.PlayerTurn)
                return;

            Card card = Deck.DrawCard();
            card.IsFaceUp = true;
            CurrentPlayer.Hand.AddCard(card);

            // Raise event for the card dealt
            CardDealt?.Invoke(card, true);

            if (CurrentPlayer.Hand.IsBusted)
            {
                if (IsTwoPlayerMode && CurrentPlayer == Player1 && Player2 != null)
                {
                    // Switch to player 2
                    CurrentPlayer = Player2;
                    OnCurrentPlayerChanged(new PlayerChangedEventArgs { NewPlayer = CurrentPlayer });
                }
                else
                {
                    // Move to dealer's turn if current player busts and it's the last player
                    DealerTurn();
                }
            }
        }

        public void Stand()
        {
            if (State != GameState.PlayerTurn)
                return;

            if (IsTwoPlayerMode && CurrentPlayer == Player1 && Player2 != null)
            {
                // Switch to player 2
                CurrentPlayer = Player2;
                OnCurrentPlayerChanged(new PlayerChangedEventArgs { NewPlayer = CurrentPlayer });
            }
            else
            {
                // Move to dealer's turn if current player stands and it's the last player
                DealerTurn();
            }
        }

        private void DealerTurn()
        {
            State = GameState.DealerTurn;
            OnGameStateChanged(new GameStateChangedEventArgs { NewState = State });

            Dealer.PlayTurn(Deck);

            // Raise events for any new dealer cards
            foreach (var card in Dealer.Hand.Cards)
            {
                if (card.IsFaceUp)
                {
                    CardDealt?.Invoke(card, false);
                }
            }

            DetermineWinners();
        }

        private void DetermineWinners()
        {
            int dealerValue = Dealer.Hand.GetValue();
            bool dealerBusted = Dealer.Hand.IsBusted;

            // Determine outcome for Player 1
            EvaluatePlayerOutcome(Player1, dealerValue, dealerBusted);

            // Determine outcome for Player 2 if in two player mode
            if (IsTwoPlayerMode && Player2 != null)
            {
                EvaluatePlayerOutcome(Player2, dealerValue, dealerBusted);
            }

            State = GameState.GameOver;
            OnGameStateChanged(new GameStateChangedEventArgs { NewState = State });
        }

        private void EvaluatePlayerOutcome(Player player, int dealerValue, bool dealerBusted)
        {
            if (player.Hand.IsBusted)
            {
                player.Lose();
                return;
            }

            if (dealerBusted)
            {
                if (player.Hand.HasBlackjack)
                {
                    player.WinBlackjack();
                }
                else
                {
                    player.Win();
                }
                return;
            }

            int playerValue = player.Hand.GetValue();

            if (playerValue > dealerValue)
            {
                if (player.Hand.HasBlackjack)
                {
                    player.WinBlackjack();
                }
                else
                {
                    player.Win();
                }
            }
            else if (playerValue < dealerValue)
            {
                player.Lose();
            }
            else
            {
                // Push (tie)
                player.Push();
            }
        }

        public async Task<bool> PlayAgain()
        {
            await Task.Delay(1500); // Give a pause between rounds
            StartNewRound();
            return true;
        }

        // Event handlers
        protected virtual void OnGameStateChanged(GameStateChangedEventArgs e)
        {
            GameStateChanged?.Invoke(this, e);
        }

        protected virtual void OnCurrentPlayerChanged(PlayerChangedEventArgs e)
        {
            CurrentPlayerChanged?.Invoke(this, e);
        }
    }

    // Event argument classes
    public class GameStateChangedEventArgs : EventArgs
    {
        public GameState NewState { get; set; }
    }

    public class PlayerChangedEventArgs : EventArgs
    {
        public Player NewPlayer { get; set; }
    }
}