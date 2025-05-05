
// BlackjackGame.Core/Models/Player.cs
using BlackjackGame.Core.Models;

namespace BlackjackGame.Core.Models
{
    public class Player
    {
        private string _id = string.Empty;


        public string Id
        {
            get => _id;
            set
            {
                // Akzeptiere auch leere IDs für die Initialisierung
                _id = value ?? string.Empty;
            }
        }
        public string Name { get; set; }
        public Hand Hand { get; } = new Hand();
        public int Balance { get; set; }
        public int CurrentBet { get; set; }

        public Player(string name, int initialBalance = 1000)
        {
            Id = string.Empty;
            Name = name;
            Balance = initialBalance;
        }

        public void PlaceBet(int amount)
        {
            if (amount <= Balance && amount > 0)
            {
                CurrentBet = amount;
                Balance -= amount;
            }
        }

        public void Win()
        {
            Balance += CurrentBet * 2; // Original bet + winnings
            CurrentBet = 0;
        }

        public void WinBlackjack()
        {
            Balance += (int)(CurrentBet * 2.5); // Original bet + winnings (1.5x)
            CurrentBet = 0;
        }

        public void Lose()
        {
            CurrentBet = 0; // Bet is already deducted from balance when placed
        }

        public void Push()
        {
            Balance += CurrentBet; // Return original bet
            CurrentBet = 0;
        }
    }
}
