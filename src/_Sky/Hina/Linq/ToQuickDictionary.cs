using System;
using System.Collections.Generic;
using System.Linq;

// csharp: hina/linq/toquickdictionary.cs [snipped]
namespace Hina.Linq
{
    partial class HinaLinq
    {
        const int DictionaryThreshold = 10;

        public static IDictionary<TKey, TSource> ToQuickDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            => ToQuickDictionary(source, keySelector, x => x, null);

        public static IDictionary<TKey, TSource> ToQuickDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
            => ToQuickDictionary(source, keySelector, x => x, comparer);

        public static IDictionary<TKey, TElement> ToQuickDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
            => ToQuickDictionary(source, keySelector, elementSelector, null);

        public static IDictionary<TKey, TElement> ToQuickDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            var count = GetCollectionLength(source);

            return count.HasValue && count < DictionaryThreshold
                ? source.ToKeyDictionary(keySelector, elementSelector, comparer)
                : source.ToDictionary(keySelector, elementSelector, comparer);
        }
    }
}
