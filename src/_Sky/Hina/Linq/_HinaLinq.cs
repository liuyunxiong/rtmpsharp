using System.Collections;
using System.Collections.Generic;

// csharp: hina/linq/_hinalinq.cs [snipped]
namespace Hina.Linq
{
    static partial class HinaLinq
    {
        public static int? GetCollectionLength<T>(IEnumerable<T> source)
        {
            switch (source)
            {
                case ICollection<T> x: return x.Count;
                case ICollection x: return x.Count;
                default: return null;
            }
        }
    }
}
