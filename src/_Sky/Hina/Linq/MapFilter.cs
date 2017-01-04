using System;
using System.Collections.Generic;
using System.Linq;

// csharp: hina/linq/mapfilter.cs [snipped]
namespace Hina.Linq
{
    partial class HinaLinq
    {
        // slightly more efficient map and filter implementation

        public static List<TResult> MapList<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(selector, nameof(selector));

            // matches corefx's list<t> default capacity
            const int DefaultCapacity = 4;

            var count = GetCollectionLength(source) ?? DefaultCapacity;
            var list  = new List<TResult>(count);

            list.AddRange(source.Select(selector));
            return list;
        }

        public static List<TResult> MapList<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(selector, nameof(selector));

            // matches corefx's list<t> default capacity
            const int DefaultCapacity = 4;

            var count = GetCollectionLength(source) ?? DefaultCapacity;
            var list  = new List<TResult>(count);

            list.AddRange(source.Select(selector));
            return list;
        }

        public static TResult[] MapArray<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, TResult> selector)
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(selector, nameof(selector));

            var count  = GetCollectionLength(source) ?? 0;
            var buffer = new Buffer<TResult>(count);

            foreach (var item in source)
                buffer.Append(selector(item));

            return buffer.ToArray();
        }

        public static TResult[] MapArray<TSource, TResult>(this IEnumerable<TSource> source, Func<TSource, int, TResult> selector)
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(selector, nameof(selector));

            var count  = GetCollectionLength(source) ?? 0;
            var buffer = new Buffer<TResult>(count);
            var index  = 0;

            foreach (var item in source)
                buffer.Append(selector(item, index++));

            return buffer.ToArray();
        }

        public static List<T> FilterList<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(predicate, nameof(predicate));

            return source.Where(predicate).ToList();
        }

        public static T[] FilterArray<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            Check.NotNull(source, nameof(source));
            Check.NotNull(predicate, nameof(predicate));

            return source.Where(predicate).ToArray();
        }
    }
}
