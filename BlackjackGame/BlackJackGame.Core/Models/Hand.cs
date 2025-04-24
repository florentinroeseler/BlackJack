
// BlackjackGame.Core/Models/Hand.cs
using System.Collections.Generic;
using System.Linq;
using BlackjackGame.Core.Models;

namespace BlackjackGame.Core.Models
{
    public class Hand
    {
        private List<Card> cards = new List<Card>();

        public IReadOnlyList<Card> Cards => cards.AsReadOnly();

        public void AddCard(Card card)
        {
            cards.Add(card);
            card.IsFaceUp = true;
        }

        public void Clear()
        {
            cards.Clear();
        }

        public int GetValue()
        {
            int value = cards.Sum(c => c.GetValue());
            int aceCount = cards.Count(c => c.Rank == Rank.Ace);

            // Wenn Gesamtwert über 21 und Asse vorhanden, zähle Asse als 1 statt 11
            while (value > 21 && aceCount > 0)
            {
                value -= 10; // Reduziere Ass von 11 auf 1
                aceCount--;
            }

            return value;
        }

        public bool IsBusted => GetValue() > 21;
        public bool HasBlackjack => cards.Count == 2 && GetValue() == 21;
    }
}
