namespace Growl
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

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
    }
}