using System;

// csharp: hina/extensions/arraysegmentextensions.cs [snipped]
namespace Hina
{
    static class ArraySegmentExtensions
    {
        public static T[] ToArray<T>(this ArraySegment<T> x)
        {
            var array = new T[x.Count];

            Array.Copy(x.Array, x.Offset, array, 0, x.Count);
            return array;
        }

        public static byte[] ToArray(this ArraySegment<byte> x)
        {
            var array = new byte[x.Count];

            Buffer.BlockCopy(x.Array, x.Offset, array, 0, x.Count);
            return array;
        }
    }
}
