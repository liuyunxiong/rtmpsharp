using System;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;

// csharp: hina/io/bytereader.cs [snipped]
namespace Hina.IO
{
    public class ByteReader
    {
        public static ByteReader Unbound => new ByteReader();


        int position;
        Space<byte> span;

        readonly Encoding encoding;


        public ByteReader()
            : this(Space<byte>.Empty, Encoding.UTF8) { }

        public ByteReader(Space<byte> span)
            : this(span, Encoding.UTF8) { }

        public ByteReader(Space<byte> span, Encoding encoding)
        {
            this.span     = span;
            this.encoding = encoding;
        }



        // helper

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        void RequireLength(int length)
        {
            if (position + length > span.Length)
                throw new EndOfStreamException();
        }



        // public

        public bool HasLength(int count)
        {
            return position + count <= span.Length;
        }

        public bool EndOfStream
        {
            get => position >= span.Length;
        }

        public Space<byte> Span
        {
            get { return span; }
            set { span = value; position = 0; }
        }

        public int Length
        {
            get => span.Length;
        }

        public int Position
        {
            get => position;
            set { if (value < 0 || value >= span.Length) throw new ArgumentOutOfRangeException(nameof(value), value, null); position = value; }
        }

        public void Reset()
        {
            position = 0;
        }


        public bool ReadBoolean()
        {
            RequireLength(1);
            return span[position++] != 0;
        }

        public byte ReadByte()
        {
            RequireLength(1);
            return span[position++];
        }

        public sbyte ReadSByte()
        {
            RequireLength(1);
            return (sbyte)span[position++];
        }

        public short ReadInt16()
        {
            RequireLength(2);

            var value = (short)(span[position + 0] | span[position + 1] << 8);

            position += 2;
            return value;
        }

        public ushort ReadUInt16()
        {
            RequireLength(2);

            var value = (ushort)(span[position + 0] | span[position + 1] << 8);

            position += 2;
            return value;
        }

        public int ReadInt32()
        {
            RequireLength(4);

            var value = (int)(span[position + 0] | span[position + 1] << 8 | span[position + 2] << 16 | span[position + 3] << 24);

            position += 4;
            return value;
        }

        public uint ReadUInt32()
        {
            RequireLength(4);

            var value = (uint)(span[position + 0] | span[position + 1] << 8 | span[position + 2] << 16 | span[position + 3] << 24);

            position += 4;
            return value;
        }

        public long ReadInt64()
        {
            RequireLength(8);

            var lo = (uint)(span[position + 0] | span[position + 1] << 8 | span[position + 2] << 16 | span[position + 3] << 24);
            var hi = (uint)(span[position + 4] | span[position + 5] << 8 | span[position + 6] << 16 | span[position + 7] << 24);
            var value = (long)(ulong)hi << 32 | lo;

            position += 8;
            return value;
        }

        public ulong ReadUInt64()
        {
            RequireLength(8);

            var lo = (uint)(span[position + 0] | span[position + 1] << 8 | span[position + 2] << 16 | span[position + 3] << 24);
            var hi = (uint)(span[position + 4] | span[position + 5] << 8 | span[position + 6] << 16 | span[position + 7] << 24);
            var value = (ulong)hi << 32 | lo;

            position += 8;
            return value;
        }

        public unsafe float ReadSingle()
        {
            RequireLength(4);

            var value = (uint)(span[position + 0] | span[position + 1] << 8 | span[position + 2] << 16 | span[position + 3] << 24);

            position += 4;
            return *(float*)&value;
        }

        public unsafe double ReadDouble()
        {
            RequireLength(8);

            var lo = (uint)(span[position + 0] | span[position + 1] << 8 | span[position + 2] << 16 | span[position + 3] << 24);
            var hi = (uint)(span[position + 4] | span[position + 5] << 8 | span[position + 6] << 16 | span[position + 7] << 24);
            var value = (ulong)hi << 32 | lo;

            position += 8;
            return *(double*)&value;

        }

        public unsafe string ReadString()
        {
            var length = Read7BitInt32();

            if (length < 0)
                throw new IOException("invalid string length", new FormatException());

            if (length == 0)
                return string.Empty;

            RequireLength(length);

            var sliced = span.Slice(position, length);
            fixed (byte* pBytes = sliced.Array)
            {
                var content = encoding.GetString(pBytes + sliced.Offset, sliced.Length);
                position += length;
                return content;
            }
        }

        public int Read7BitInt32()
        {
            // read an int32 7 bits at a time. the high bit of the byte when on means to continue reading more bytes.
            var count = 0;
            var shift = 0;
            byte b;

            do
            {
                // check for a corrupted stream. read a max of 5 bytes.
                // at most 5 bytes needed to represent an int32; shift += 7
                if (shift == 5 * 7)
                    throw new FormatException();

                // readbyte throws on end of stream
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);

            return count;
        }

        public int Read(Space<byte> span)
        {
            return Read(span.Array, span.Offset, span.Length);
        }

        public int Read(byte[] buffer, int index, int count)
        {
            if (buffer == null) throw new ArgumentNullException(nameof(buffer));
            if (index < 0)      throw new ArgumentOutOfRangeException(nameof(index), index, null);
            if (count < 0)      throw new ArgumentOutOfRangeException(nameof(count), count, null);
            if (index + count > buffer.Length) throw new ArgumentException("invalid count (length)");

            return ReadUnchecked(buffer, index, count);
        }

        public int ReadUnchecked(byte[] buffer, int index, int count)
        {
            var copy = Math.Min(count, span.Length - position);

            if (copy > 0)
            {
                var slice       = span.Slice(position, copy);
                var destination = new Space<byte>(buffer, index, count);

                slice.CopyTo(destination);
                position += copy;
                return copy;
            }
            else
            {
                return 0;
            }
        }

        public byte[] ReadBytes(int count)
        {
            return ReadSpan(count).ToArray();
        }

        public Space<byte> ReadSpan(int count)
        {
            RequireLength(count);

            var x = span.Slice(position, count);
            position += count;

            return x;
        }
    }
}