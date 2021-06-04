namespace Growl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using Services;

    public static class EnumerableExtensionMethods
    {
        public static IEnumerable<TItem> Shuffle<TItem>(this IEnumerable<TItem> enumerable)
        {
            var enumeratedItems = enumerable.ToList();
            var random = new Random();
            
            while(enumeratedItems.Any())
            {
                var index = random.Next(0, enumeratedItems.Count);
                var result = enumeratedItems[index];
                enumeratedItems.RemoveAt(index);

                yield return result;
            }
        }

        public static (IEnumerable<PlayerState> Players, IEnumerable<ICard> Deck) Deal(this IEnumerable<ICard> cards, IEnumerable<PlayerState> players, int countPerPlayer) =>
            cards.DealToMaximum(players, countPerPlayer, int.MaxValue);
        
        public static (IEnumerable<PlayerState> Players, IEnumerable<ICard> Deck) DealToMaximum(this IEnumerable<ICard> cards, IEnumerable<PlayerState> players, int countPerPlayer, int maxHandSize)
        {
            var enumeratedPlayers = players.ToArray();
            var cardQueue = new Queue<ICard>(cards);

            for (var i = 0; i < countPerPlayer; ++i)
            {
                for (var p = 0; p < enumeratedPlayers.Length; ++p)
                {
                    if (!cardQueue.TryDequeue(out var card))
                        break;

                    if (enumeratedPlayers[p].Hand.Count() >= maxHandSize)
                        continue;

                    enumeratedPlayers[p] = enumeratedPlayers[p] with
                    {
                        Hand = enumeratedPlayers[p].Hand.Append(card),
                    };
                }
            }

            return (
                enumeratedPlayers,
                cardQueue);
        }

        public static IEnumerable<TItem> Dequeue<TItem>(this IEnumerable<TItem> queue, out TItem item)
        {
            item = queue.First();
            return queue.Skip(1);
        }

        public static IEnumerable<TItem> Enqueue<TItem>(this IEnumerable<TItem> queue, TItem item) =>
            queue.Append(item);

        public static IEnumerable<TItem> Enqueue<TItem>(this IEnumerable<TItem> queue, IEnumerable<TItem> items) =>
            queue.Concat(items);

        public static IEnumerable<TItem> TakeBetween<TItem>(this IEnumerable<TItem> enumerable, int startIndex, int endIndex) =>
            enumerable.Skip(startIndex).Take(endIndex - startIndex);
    }
}