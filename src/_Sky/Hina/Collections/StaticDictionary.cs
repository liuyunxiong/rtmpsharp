using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Hina.Linq;

// csharp: hina/collections/staticdictionary.cs [snipped]
namespace Hina.Collections
{
    // a small and compact dictionary intended for small, static lookups. adding multiple items with the same key is
    // undefined behaivour.
    class StaticDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        const int DefaultCapacity = 4;


        int     count;
        Entry[] entries;
        readonly IEqualityComparer<TKey> comparer;

        public int                 Count  => count;
        public ICollection<TKey>   Keys   => entries.MapArray(x => x.Key);
        public ICollection<TValue> Values => entries.MapArray(x => x.Value);


        public StaticDictionary()
            : this(DefaultCapacity) { }

        public StaticDictionary(int capacity)
            : this(capacity, null) { }

        public StaticDictionary(IEqualityComparer<TKey> comparer)
            : this(DefaultCapacity, comparer) { }

        public StaticDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
            this.entries  = new Entry[capacity];
            this.count    = 0;
        }


        // support the dictionary initializer syntax
        public void Add(TKey key, TValue value)
        {
            Check.NotNull(key);

            if (entries.Length == count)
            {
                Array.Resize(
                    ref entries,
                    Math.Max(count * 2, DefaultCapacity));
            }

            entries[count++] = new Entry(key, value);
        }

        public bool ContainsKey(TKey key)
        {
            foreach (var e in entries)
            {
                if (comparer.Equals(e.Key, key))
                    return true;
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            Check.NotNull(key);

            for (var i = 0; i < count; i++)
            {
                var e = entries[i];

                if (comparer.Equals(e.Key, key))
                {
                    value = e.Value;
                    return true;
                }
            }

            value = default(TValue);
            return false;
        }

        public TValue this[TKey key]
        {
            get
            {
                Check.NotNull(key);

                for (var i = 0; i < count; i++)
                {
                    var e = entries[i];

                    if (comparer.Equals(e.Key, key))
                        return e.Value;
                }

                throw new KeyNotFoundException();
            }
            set
            {
                throw new NotSupportedException();
            }
        }


        #region idictionary<k, v> members

        IEnumerator<KeyValuePair<TKey, TValue>> Enumerator                                                      => entries.Select(x => new KeyValuePair<TKey, TValue>(x.Key, x.Value)).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()                                                                 => Enumerator;
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()         => Enumerator;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly                                                 => true;
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)                       => Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)                  => TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(item.Value, value);
        void ICollection<KeyValuePair<TKey, TValue>>.Clear()                                                    => throw new NotSupportedException();
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotSupportedException();
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)                    => throw new NotSupportedException();
        bool IDictionary<TKey, TValue>.Remove(TKey key)                                                         => throw new NotSupportedException();

        #endregion


        struct Entry
        {
            public readonly TKey   Key;
            public readonly TValue Value;

            public Entry(TKey key, TValue value) { Key = key; Value = value; }
        }
    }
}
