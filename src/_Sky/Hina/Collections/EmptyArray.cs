// csharp: hina/collections/emptyarray.cs [snipped]
namespace Hina
{
    // use `EmptyArray<T>.Instance` to prevent lots of zero-element array allocations.
    static class EmptyArray<T>
    {
        public static readonly T[] Instance = new T[0];
    }
}
