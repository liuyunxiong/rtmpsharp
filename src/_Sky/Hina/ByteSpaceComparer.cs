using System.Collections.Generic;

// csharp: hina/bytespacecomparer.cs [snipped]
namespace Hina
{
    class ByteSpaceComparer : IEqualityComparer<Space<byte>>
    {
        public static readonly ByteSpaceComparer Instance = new ByteSpaceComparer();

        // `value` may be null
        int IEqualityComparer<Space<byte>>.GetHashCode(Space<byte> value)
        {
            if (value.Array == null)
                return 0;

            // http://stackoverflow.com/questions/16340/how-do-i-generate-a-hashcode-from-a-byte-array-in-c-sharp/468084#468084
            unchecked
            {
                const int p = 16777619;
                var hash = (int)2166136261;

                for (var i = 0; i < value.Length; i++)
                    hash = (hash ^ value[i]) * p;

                hash += hash << 13;
                hash ^= hash >> 7;
                hash += hash << 3;
                hash ^= hash >> 17;
                hash += hash << 5;
                return hash;
            }
        }

        // `x` and `y` may be null
        bool IEqualityComparer<Space<byte>>.Equals(Space<byte> x, Space<byte> y)
        {
            return IsEqual(x, y);
        }

        // `x` and `y` may be null
        public static unsafe bool IsEqual(Space<byte> x, Space<byte> y)
        {
            if (x.Array == null || y.Array == null || x.Length != y.Length)
                return x == y;

            fixed (byte* pX = x.Array)
            fixed (byte* pY = y.Array)
                return IsEqual(pX + x.Offset, pY + y.Offset, x.Length);
        }

        static unsafe bool IsEqual(byte* x, byte* y, int length)
        {
            var last = x + length;
            var last32 = last - 32;

            // unrolled loop: compare 32 bytes at a time
            while (x < last32)
            {
                if (*(ulong*)x != *(ulong*)y)
                    return false;

                if (*(ulong*)(x + 8) != *(ulong*)(y + 8))
                    return false;

                if (*(ulong*)(x + 16) != *(ulong*)(y + 16))
                    return false;

                if (*(ulong*)(x + 24) != *(ulong*)(y + 24))
                    return false;

                x += 32;
                y += 32;
            }

            while (x < last)
            {
                if (*x != *y)
                    return false;

                ++x;
                ++y;
            }

            return true;
        }
    }
}
