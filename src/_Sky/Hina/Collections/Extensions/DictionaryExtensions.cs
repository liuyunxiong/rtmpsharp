using System.Collections.Generic;

// csharp: hina/collections/extensions/dictionaryextensions.cs [snipped]
namespace Hina.Collections
{
    static class DictionaryExtensions
    {
        public static TValue GetDefault<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            Check.NotNull(dictionary);
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public static TValue GetDefaultForReadOnlyList<TKey, TValue>(this IReadOnlyDictionary<TKey, TValue> dictionary, TKey key, TValue defaultValue = default(TValue))
        {
            Check.NotNull(dictionary);
            return dictionary.TryGetValue(key, out var value) ? value : defaultValue;
        }
    }
}
