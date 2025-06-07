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
            Card card = deck.DrawCard();
            card.IsFaceUp = true;
            player.Hand.AddCard(card);

            card = deck.DrawCard();
            card.IsFaceUp = true;
            Hand.AddCard(card);

            card = deck.DrawCard();
            card.IsFaceUp = true;
            player.Hand.AddCard(card);

            card = deck.DrawCard();
            card.IsFaceUp = false;
            Hand.AddCard(card);
        }

        public void DealInitialCards(Deck deck, Player player1, Player player2)
        {
            Card card = deck.DrawCard();
            card.IsFaceUp = true;
            player1.Hand.AddCard(card);

            card = deck.DrawCard();
            card.IsFaceUp = true;
            player2.Hand.AddCard(card);

            card = deck.DrawCard();
            card.IsFaceUp = true;
            Hand.AddCard(card);

            card = deck.DrawCard();
            card.IsFaceUp = true;
            player1.Hand.AddCard(card);

            card = deck.DrawCard();
            card.IsFaceUp = true;
            player2.Hand.AddCard(card);

            card = deck.DrawCard();
            card.IsFaceUp = false;
            Hand.AddCard(card);
        }

        public void PlayTurn(Deck deck)
        {
            RevealHiddenCard();

            while (Hand.GetValue() < 17)
            {
                Card card = deck.DrawCard();
                card.IsFaceUp = true;
                Hand.AddCard(card);
            }
        }
    }
}
