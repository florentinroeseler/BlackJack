// BlackjackGame.Core/Models/Card.cs
using BlackjackGame.Core.Models;

namespace BlackjackGame.Core.Models
{
    public enum Suit
    {
        Hearts,
        Diamonds,
        Clubs,
        Spades
    }

    public enum Rank
    {
        Two = 2,
        Three = 3,
        Four = 4,
        Five = 5,
        Six = 6,
        Seven = 7,
        Eight = 8,
        Nine = 9,
        Ten = 10,
        Jack = 11,
        Queen = 12,
        King = 13,
        Ace = 14
    }

    public class Card
    {
        public Suit Suit { get; }
        public Rank Rank { get; }
        public bool IsFaceUp { get; set; }

        public Card(Rank rank, Suit suit)
        {
            Rank = rank;
            Suit = suit;
            IsFaceUp = false;
        }

        public int GetValue()
        {
            if (Rank >= Rank.Jack && Rank <= Rank.King)
                return 10;
            if (Rank == Rank.Ace)
                return 11; // Ass wird initial als 11 gewertet (später ggf. als 1)
            return (int)Rank;
        }

        public override string ToString()
        {
            return $"{Rank} of {Suit}";
        }
    }
}
