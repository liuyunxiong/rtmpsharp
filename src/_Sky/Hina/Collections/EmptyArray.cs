// csharp: hina/collections/emptyarray.cs [snipped]
namespace Hina
{
    static class EmptyArray<T>
    {
        public static readonly T[] Instance = new T[0];
    }
}
