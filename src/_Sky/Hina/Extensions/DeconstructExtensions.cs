using System.Collections;
using System.Collections.Generic;

// csharp: hina/extensions/deconstructextensions.cs [snipped]
namespace Hina
{
    static class DeconstructExtensions
    {
        public static void Deconstruct<TKey, TValue>(this KeyValuePair<TKey, TValue> x, out TKey key, out TValue value)
        {
            key   = x.Key;
            value = x.Value;
        }

        public static void Deconstruct<TKey, TValue>(this DictionaryEntry x, out object key, out object value)
        {
            key   = x.Key;
            value = x.Value;
        }
    }
}
