using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Konseki;

// csharp: hina/collections/keydictionary.cs [snipped]
namespace Hina.Collections
{
    // a small and compact dictionary that uses an array for lookups. does not allocate a large
    // heap, and very fast for lookups in small dictionaries.
    class KeyDictionary<TKey, TValue> : IDictionary<TKey, TValue>
    {
        const int DefaultCapacity = 4;


        int     count;
        Entry[] entries;
        readonly IEqualityComparer<TKey> comparer;

        public int                 Count  => count;
        public ICollection<TKey>   Keys   => entries.Where(x => x.Exists).Select(x => x.Key).ToList();
        public ICollection<TValue> Values => entries.Where(x => x.Exists).Select(x => x.Value).ToList();


        public KeyDictionary()
            : this(DefaultCapacity) { }

        public KeyDictionary(int capacity)
            : this(capacity, null) { }

        public KeyDictionary(IEqualityComparer<TKey> comparer)
            : this(DefaultCapacity, comparer) { }

        public KeyDictionary(int capacity, IEqualityComparer<TKey> comparer)
        {
            this.comparer = comparer ?? EqualityComparer<TKey>.Default;
            this.entries  = new Entry[capacity];
            this.count    = 0;
        }


        public void Add(TKey key, TValue value)
        {
            RequireNonNullUniqueKey(key);

            if (entries.Length == count)
                GrowStorage();

            for (var i = 0; i < entries.Length; i++)
            {
                if (entries[i].Exists)
                    continue;

                entries[i] = new Entry(key, value);
                count++;
                return;
            }
        }



        public void AddRange(IList<(TKey, TValue)> list)
            => AddRangeInternal(
                list,
                list.Count);

        public void AddRange(IList<KeyValuePair<TKey, TValue>> list)
            => AddRangeInternal(
                list.Select(x => (x.Key, x.Value)),
                list.Count);

        public void AddRange(IEnumerable<(TKey, TValue)> enumerable, int lengthHint = -1)
            => AddRangeInternal(
                Check.NotNull(enumerable),
                lengthHint != -1 ? lengthHint : RequireMultipleEnumerationGetCount(enumerable));

        public void AddRange(IEnumerable<KeyValuePair<TKey, TValue>> enumerable, int lengthHint = -1)
            => AddRangeInternal(
                Check.NotNull(enumerable).Select(x => (x.Key, x.Value)),
                lengthHint != -1 ? lengthHint : RequireMultipleEnumerationGetCount(enumerable));

        // `addrangeinternal` is guarenteed toenumerates through the items range only once
        void AddRangeInternal(IEnumerable<(TKey key, TValue value)> items, int length)
        {
            if (length == 0)
                return;

            var newLength = count + length;

            if (entries.Length < newLength)
                Array.Resize(ref entries, newLength);

            var i = 0;
            var e = items.GetEnumerator();

            while (e.MoveNext() && i < newLength)
            {
                var (key, value) = e.Current;

                RequireNonNullUniqueKey(key);

                if (entries[i].Exists)
                {
                    i++;
                }
                else
                {
                    entries[i] = new Entry(key, value);

                    count++;
                    i++;
                }
            }
        }


        public void Clear()
        {
            for (var i = 0; i < entries.Length; i++)
                entries[i] = default(Entry);

            count = 0;
        }

        public bool ContainsKey(TKey key)
        {
            foreach (var e in entries)
            {
                if (e.Exists && comparer.Equals(e.Key, key))
                    return true;
            }

            return false;
        }

        public bool Remove(TKey key)
        {
            Check.NotNull(key);

            for (var i = 0; i < entries.Length; i++)
            {
                var e = entries[i];

                if (e.Exists && comparer.Equals(e.Key, key))
                {
                    entries[i] = default(Entry);
                    count--;
                    return true;
                }
            }

            return false;
        }

        public bool TryGetValue(TKey key, out TValue value)
        {
            Check.NotNull(key);

            for (var i = 0; i < entries.Length; i++)
            {
                var e = entries[i];

                if (e.Exists && comparer.Equals(e.Key, key))
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

                for (var i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];

                    if (e.Exists && comparer.Equals(e.Key, key))
                        return e.Value;
                }

                throw new KeyNotFoundException();
            }
            set
            {
                Check.NotNull(key);

                var free = -1;

                for (var i = 0; i < entries.Length; i++)
                {
                    var e = entries[i];

                    if (e.Exists)
                    {
                        if (comparer.Equals(e.Key, key))
                        {
                            entries[i] = new Entry(key, value);
                            return;
                        }
                    }
                    else if (free == -1)
                    {
                        free = i;
                    }
                }

                if (free != -1)
                {
                    entries[free] = new Entry(key, value);
                    count++;
                }
                else
                {
                    var previous = entries.Length;

                    GrowStorage();
                    Kon.Assert(entries[previous].Exists == false);

                    entries[previous] = new Entry(key, value);
                    count++;
                }
            }
        }


        #region helpers

        void RequireNonNullUniqueKey(TKey key)
        {
            Check.NotNull(key);

            if (ContainsKey(key))
                throw new ArgumentException("an element with the same key already exists in this dictionary");
        }

        static int RequireMultipleEnumerationGetCount<T>(IEnumerable<T> enumerable)
        {
            var count = enumerable.Count();

            // guard against enumerables that don't allow multiple enumeration
            if (count == enumerable.Count())
                return count;

            throw new InvalidOperationException("enumerable does not support multiple enumeration");
        }

        void GrowStorage()
        {
            if (entries.Length == count)
            {
                Array.Resize(
                    ref entries,
                    Math.Max(entries.Length * 2, DefaultCapacity));
            }
        }

        #endregion


        #region idictionary<k, v> members


        IEnumerator<KeyValuePair<TKey, TValue>> Enumerator                                                      => entries.Where(x => x.Exists).Select(x => new KeyValuePair<TKey, TValue>(x.Key, x.Value)).GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator()                                                                 => Enumerator;
        IEnumerator<KeyValuePair<TKey, TValue>> IEnumerable<KeyValuePair<TKey, TValue>>.GetEnumerator()         => Enumerator;

        bool ICollection<KeyValuePair<TKey, TValue>>.IsReadOnly                                                 => false;
        void ICollection<KeyValuePair<TKey, TValue>>.Add(KeyValuePair<TKey, TValue> item)                       => Add(item.Key, item.Value);
        bool ICollection<KeyValuePair<TKey, TValue>>.Contains(KeyValuePair<TKey, TValue> item)                  => TryGetValue(item.Key, out var value) && EqualityComparer<TValue>.Default.Equals(item.Value, value);
        void ICollection<KeyValuePair<TKey, TValue>>.CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => throw new NotSupportedException();
        bool ICollection<KeyValuePair<TKey, TValue>>.Remove(KeyValuePair<TKey, TValue> item)                    => throw new NotSupportedException();

        #endregion


        struct Entry
        {
            public readonly bool   Exists;
            public readonly TKey   Key;
            public readonly TValue Value;

            public Entry(TKey key, TValue value)
            {
                Key    = key;
                Value  = value;
                Exists = true;
            }
        }
    }
}
