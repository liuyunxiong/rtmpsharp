using System;
using System.Runtime.CompilerServices;

// obsolete member overrides non-obsolete member - we are intentionally deprecating gethashcode() on space<t>
#pragma warning disable CS0809

// csharp: hina/space.cs [snipped]
namespace Hina
{
    public struct Space<T>
    {
        public static readonly Space<T> Empty = new Space<T>(new T[0]);


        readonly T[] array;
        readonly int offset;
        readonly int length;

        public T[] Array    => array;
        public int Offset   => offset;
        public int Length   => length;
        public bool IsEmpty => length == 0;


        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Space(T[] array)
        {
            Check.NotNull(array);

            this.array  = array;
            this.offset = 0;
            this.length = array.Length;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Space(T[] array, int start)
        {
            Check.NotNull(array);

            var arrayLength = array.Length;

            if ((uint)start > (uint)arrayLength)
                throw OutOfRangeException();

            this.array  = array;
            this.offset = 0;
            this.length = arrayLength - start;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Space(T[] array, int start, int length)
        {
            var arrayLength = array.Length;

            if ((uint)start > (uint)arrayLength)
                throw OutOfRangeException();

            if ((uint)start > (uint)arrayLength || (uint)length > arrayLength - start)
                throw OutOfRangeException();

            this.array  = array;
            this.offset = start;
            this.length = length;
        }


        // todo: https://github.com/dotnet/corefx/issues/13681. this indexer currently returns a `T` instead of a `ref T`
        public T this[int index]
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)] get => array[offset + index];
            [MethodImpl(MethodImplOptions.AggressiveInlining)] set => array[offset + index] = value;
        }

        // todo: https://github.com/dotnet/corefx/issues/13681. this temporary method will simulate the intended
        /// `ref T` indexer for those who need bypass the workaround for performance.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ref T GetItem(int index) => ref array[offset + index];

        // copies the contents of this span into destination span. If the source and destinations overlap, this method
        // behaves as if the original values in a temporary location before the destination is overwritten.
        public void CopyTo(Space<T> destination)
        {
            if (!TryCopyTo(destination))
                throw new ArgumentException("the destination space is too short");
        }


        // copies the contents of this span into destination span. if the source and destinations overlap, this method
        // behaves as if the original values in a temporary location before the destination is overwritten.
        //
        // if the destination span is shorter than the source span, this method return false and no data is written to
        // the destination.
        public unsafe bool TryCopyTo(Space<T> destination)
        {
            CheckDebug.NotNull(array, destination.array);

            if ((uint)length > (uint)destination.length)
                return false;

            if (typeof(T) == typeof(byte))
            {
                if (array.Length > 0 && destination.array.Length > 0)
                {
                    fixed (byte* pSource      = array as byte[])
                    fixed (byte* pDestination = destination.array as byte[])
                    {
                        Buffer.MemoryCopy(
                            source:                 pSource      + offset,
                            destination:            pDestination + destination.offset,
                            destinationSizeInBytes: destination.length,
                            sourceBytesToCopy:      length);
                    }
                }

                return true;
            }
            else
            {
                System.Array.Copy(array, destination.array, length);
                return true;
            }
        }

        // returns true if left and right point at the same memory and have the same length. this does *not* check to
        // see if the *contents* are equal.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Space<T> left, Space<T> right)
            => left.array == right.array && left.length == right.length;

        // returns false if left and right point at the same memory and have the same length. this does *not* check to
        // see if the *contents* are equal.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Space<T> left, Space<T> right)
            => !(left == right);

        // returns true if left and right point at the same memory and have the same length. this does *not* check to
        // see if the *contents* are equal.
        public override bool Equals(object obj)
            => obj is Space<T> other && other == this;

        // this method is not supported as spans cannot be boxed.
        [Obsolete("GetHashCode() on Span will always throw an exception.")]
        public override int GetHashCode()
            => throw new NotSupportedException("GetHashCode() on Space<T> is not supported.");


        public static implicit operator Space<T>(T[] array)               => new Space<T>(array);
        public static implicit operator Space<T>(ArraySegment<T> segment) => new Space<T>(segment.Array, segment.Offset, segment.Count);


        // forms a slice out of the given span, beginning at 'start'.
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Space<T> Slice(int start)
        {
            if ((uint)start > (uint)length)
                throw OutOfRangeException();

            return new Space<T>(array, offset + start, length - start);
        }

        // forms a slice out of the given span, beginning at 'start', of given length
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Space<T> Slice(int start, int length)
        {
            if ((uint)start > (uint)this.length || (uint)length > (uint)(this.length - start))
                throw OutOfRangeException();

            return new Space<T>(array, offset + start, length);
        }

        /// <summary>
        /// Copies the contents of this span into a new array.  This heap
        /// allocates, so should generally be avoided, however it is sometimes
        /// necessary to bridge the gap with APIs written in terms of arrays.
        /// </summary>
        public T[] ToArray()
        {
            if (length == 0)
                return EmptyArray<T>.Instance;

            var copy = new T[length];
            CopyTo(copy);

            return copy;
        }


        static Exception OutOfRangeException() => new ArgumentOutOfRangeException("arguments provided to space<T> aren't within the array");
    }
}
