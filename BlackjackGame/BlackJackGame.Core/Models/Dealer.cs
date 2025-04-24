
// BlackjackGame.Core/Models/Dealer.cs
using BlackjackGame.Core.Models;

namespace BlackjackGame.Core.Models
{
    public class Dealer
    {
        public Hand Hand { get; } = new Hand();

        public void RevealHiddenCard()
        {
            if (Hand.Cards.Count > 0)
            {
                foreach (var card in Hand.Cards)
                {
                    card.IsFaceUp = true;
                }
            }
        }

        public void DealInitialCards(Deck deck, Player player)
        {
            // Player gets first card face up
            Card card = deck.DrawCard();
            card.IsFaceUp = true;
            player.Hand.AddCard(card);

            // Dealer gets first card face up
            card = deck.DrawCard();
            card.IsFaceUp = true;
            Hand.AddCard(card);

            // Player gets second card face up
            card = deck.DrawCard();
            card.IsFaceUp = true;
            player.Hand.AddCard(card);

            // Dealer gets second card face down (will be revealed later)
            card = deck.DrawCard();
            card.IsFaceUp = false; // Face down
            Hand.AddCard(card);
        }

        public void DealInitialCards(Deck deck, Player player1, Player player2)
        {
            // Player 1 gets first card face up
            Card card = deck.DrawCard();
            card.IsFaceUp = true;
            player1.Hand.AddCard(card);

            // Player 2 gets first card face up
            card = deck.DrawCard();
            card.IsFaceUp = true;
            player2.Hand.AddCard(card);

            // Dealer gets first card face up
            card = deck.DrawCard();
            card.IsFaceUp = true;
            Hand.AddCard(card);

            // Player 1 gets second card face up
            card = deck.DrawCard();
            card.IsFaceUp = true;
            player1.Hand.AddCard(card);

            // Player 2 gets second card face up
            card = deck.DrawCard();
            card.IsFaceUp = true;
            player2.Hand.AddCard(card);

            // Dealer gets second card face down (will be revealed later)
            card = deck.DrawCard();
            card.IsFaceUp = false; // Face down
            Hand.AddCard(card);
        }

        public void PlayTurn(Deck deck)
        {
            RevealHiddenCard();

            // Dealer must draw until reaching 17 or more
            while (Hand.GetValue() < 17)
            {
                Card card = deck.DrawCard();
                card.IsFaceUp = true;
                Hand.AddCard(card);
            }
        }
    }
}
