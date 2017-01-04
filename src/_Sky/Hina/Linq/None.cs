using System;
using System.Collections.Generic;
using System.Linq;

// csharp: hina/linq/none.cs [snipped]
namespace Hina.Linq
{
    partial class HinaLinq
    {
        public static bool None<T>(this IEnumerable<T> source, Func<T, bool> predicate)
        {
            Check.NotNull(source, nameof(source));

            return source.All(x => !predicate(x));
        }

        public static bool None<T>(this IEnumerable<T> source)
        {
            Check.NotNull(source, nameof(source));

            return !source.Any();
        }
    }
}
