using System.Collections.Generic;

// csharp: hina/collections/emptycollection.cs [snipped]
namespace Hina
{
    // empty singleton collections of T
    static class EmptyCollection<T>
    {
        public static readonly T[]              Array        = EmptyArray<T>.Instance;
        public static readonly List<T>          List         = new List<T>(0);
        public static readonly IReadOnlyList<T> ReadOnlyList = new List<T>(0);
    }
}
