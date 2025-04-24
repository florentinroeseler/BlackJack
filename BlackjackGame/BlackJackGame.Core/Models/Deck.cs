
// BlackjackGame.Core/Models/Deck.cs
using System;
using System.Collections.Generic;
using System.Linq;
using BlackjackGame.Core.Models;

namespace BlackjackGame.Core.Models
{
    public class Deck
    {
        private List<Card> cards;
        private readonly Random random = new Random();

        public Deck()
        {
            Initialize();
        }

        public void Initialize()
        {
            cards = new List<Card>();
            foreach (Suit suit in Enum.GetValues(typeof(Suit)))
            {
                foreach (Rank rank in Enum.GetValues(typeof(Rank)))
                {
                    cards.Add(new Card(rank, suit));
                }
            }
            Shuffle();
        }

        public void Shuffle()
        {
            cards = cards.OrderBy(c => random.Next()).ToList();
        }

        public Card DrawCard()
        {
            if (cards.Count == 0)
            {
                Initialize();
            }

            Card card = cards[0];
            cards.RemoveAt(0);
            return card;
        }

        public int RemainingCards => cards.Count;
    }
}
