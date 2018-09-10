using System;
using System.Collections.Generic;
using System.Linq;
using Hina.Collections;

// csharp: hina/linq/tokeydictionary.cs [snipped]
namespace Hina.Linq
{
    partial class HinaLinq
    {
        public static IDictionary<TKey, TSource> ToKeyDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector)
            => ToKeyDictionary(source, keySelector, x => x, null);

        public static IDictionary<TKey, TSource> ToKeyDictionary<TSource, TKey>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, IEqualityComparer<TKey> comparer)
            => ToKeyDictionary(source, keySelector, x => x, comparer);

        public static IDictionary<TKey, TElement> ToKeyDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector)
            => ToKeyDictionary(source, keySelector, elementSelector, null);

        // `comparer` may be null
        public static IDictionary<TKey, TElement> ToKeyDictionary<TSource, TKey, TElement>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TElement> elementSelector, IEqualityComparer<TKey> comparer)
        {
            Check.NotNull(source, keySelector, elementSelector);

            var elements   = source.Select(x => (keySelector(x), elementSelector(x)));
            var dictionary = new KeyDictionary<TKey, TElement>(comparer);

            dictionary.AddRange(elements, HinaLinq.GetCollectionLength(elements) ?? -1);
            return dictionary;
        }
    }
}
