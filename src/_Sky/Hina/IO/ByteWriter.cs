using System;
using System.Buffers;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// csharp: hina/io/bytewriter.cs [snipped]
namespace Hina.IO
{
    public class ByteWriter
    {
        public static ByteWriter Unbound => new ByteWriter();


        const int TemporaryBufferLength = 8;
        const int DefaultBufferLength = 64;


        int position;
        Space<byte> buffer;

        readonly Encoding encoding;
        readonly Encoder  encoder;
        readonly bool     managed;
        readonly byte[]   temporary;


        // automatically managed

        public ByteWriter()
            : this(Rent(DefaultBufferLength), managed: true) { }

        public ByteWriter(int size)
            : this(Rent(size), managed: true) { }

        public ByteWriter(Encoding encoding)
            : this(Rent(DefaultBufferLength), encoding, managed: true) { }

        public ByteWriter(int size, Encoding encoding)
            : this(Rent(size), encoding, managed: true) { }


        // with `span` + `managed` option
        //     [2016-12-23] if `managed` is true, then the array must come from the array pool instance. if it does
        //     not, the array pool will throw when we try to return it.

        public ByteWriter(Space<byte> span, bool managed = false)
            : this(span, Encoding.UTF8, managed) { }

        public ByteWriter(Space<byte> span, Encoding encoding, bool managed = false)
        {
            Check.NotNull(span, encoding);

            this.buffer    = span;
            this.encoding  = encoding;
            this.managed   = managed;
            this.encoder   = encoding.GetEncoder();
            this.temporary = new byte[TemporaryBufferLength];
        }



        // helpers

        static byte[] Rent(int count)       => ArrayPool<byte>.Shared.Rent(count);
        static void   Return(byte[] buffer) => ArrayPool<byte>.Shared.Return(buffer);

        unsafe void RequireLength(int length)
        {
            var source    = buffer;
            var available = source.Length - position;
            var requested = length;
            var required  = requested - available;

            if (required > 0)
            {
                if (!managed)
                    throw new EndOfStreamException();

                var pool = ArrayPool<byte>.Shared;

                if (source.Length != 0)
                    pool.Return(source.Array);

                var newLength = Math.Max(
                    source.Length * 2,
                    source.Length + required);

                var destination = pool.Rent(newLength);

                if (source.Length > 0 && destination.Length > 0)
                {
                    fixed (byte* pSource      = &source.Array[source.Offset])
                    fixed (byte* pDestination = destination)
                    {
                        System.Buffer.MemoryCopy(
                            source:                 pSource,
                            destination:            pDestination,
                            destinationSizeInBytes: destination.Length,
                            sourceBytesToCopy:      source.Length);
                    }
                }

                buffer = new Space<byte>(destination);
            }
        }

        void CopyTemporary(int count)
        {
            RequireLength(count);

            for (var i = 0; i < count; i++)
                buffer[i] = temporary[i];

            position += count;
        }

        static int Get7BitEncodedSize(int value)
        {
            if (value < 0x80)       return 1;
            if (value < 0x4000)     return 2;
            if (value < 0x200000)   return 3;
            if (value < 0x10000000) return 4;

            return 5;
        }



        // public

        public void Return()
        {
            if (managed && buffer.Length != 0)
                ArrayPool<byte>.Shared.Return(buffer.Array);
        }

        public bool EndOfStream
        {
            get => position >= buffer.Length;
        }

        public Space<byte> Span
        {
            get => buffer.Slice(0, position);
        }

        public Space<byte> Buffer
        {
            get { return buffer; }
            set { if (managed) throw new InvalidOperationException(); buffer = value; position = 0; }
        }

        public int Length
        {
            get => buffer.Length;
        }

        public int Position
        {
            get { return position; }
            set { if (value < 0 || value >= buffer.Length) throw new ArgumentOutOfRangeException(nameof(value), value, null); position = value; }
        }

        public void Reset()
        {
            position = 0;
        }


        public void WriteBool(bool value)                               => Write(value);
        public void WriteByte(byte value)                               => Write(value);
        public void WriteSByte(sbyte value)                             => Write(value);
        public void WriteBytes(Space<byte> span)                        => Write(span);
        public void WriteBytes(byte[] buffer)                           => Write(buffer);
        public void WriteBytes(byte[] buffer, int index, int count)     => Write(buffer, index, count);
        public void WriteChar(char character)                           => Write(character);
        public void WriteChars(char[] characters)                       => Write(characters);
        public void WriteChars(char[] characters, int index, int count) => Write(characters, index, count);
        public void WriteDouble(double value)                           => Write(value);
        public void WriteInt16(short value)                             => Write(value);
        public void WriteUInt16(ushort value)                           => Write(value);
        public void WriteInt32(int value)                               => Write(value);
        public void WriteUInt32(uint value)                             => Write(value);
        public void WriteInt64(long value)                              => Write(value);
        public void WriteUInt64(ulong value)                            => Write(value);
        public void WriteSingle(float value)                            => Write(value);
        public void WriteString(string value)                           => Write(value);


        public void Write(bool value)
        {
            RequireLength(1);
            buffer[position++] = (byte)(value ? 1 : 0);
        }

        public void Write(byte value)
        {
            RequireLength(1);
            buffer[position++] = value;
        }

        public void Write(sbyte value)
        {
            RequireLength(1);
            buffer[position++] = (byte)value;
        }

        public void Write(byte[] buffer)
        {
            WriteBytesInternal(buffer, 0, buffer.Length);
        }

        public void Write(byte[] buffer, int index, int count)
        {
            if (index + count > buffer.Length)
                throw new ArgumentOutOfRangeException("index + count exceeds the length of the array");

            WriteBytesInternal(buffer, index, count);
        }

        unsafe void WriteBytesInternal(byte[] buffer, int index, int count)
        {
            Check.NotNull(buffer);

            RequireLength(count);

            if (buffer.Length > 0 && this.buffer.Length > 0)
            {
                fixed (byte* pSource      = &buffer[index])
                fixed (byte* pDestination = &this.buffer.Array[this.buffer.Offset + position])
                {
                    System.Buffer.MemoryCopy(
                        source:                 pSource,
                        destination:            pDestination,
                        destinationSizeInBytes: this.buffer.Length - position,
                        sourceBytesToCopy:      count);
                }
            }

            position += count;
        }

        public void Write(Space<byte> span)
        {
            WriteBytesInternal(span.Array, span.Offset, span.Length);
        }

        public void Write(char character)
        {
            if (char.IsSurrogate(character))
                throw new ArgumentException("can't write singular surrogate character");

            // .net core doesn't have this overload available.
            //     var bytes = 0;
            //     fixed (byte* pTemporary = temporary)
            //         bytes = encoder.GetBytes(&character, 1, pTemporary, TemporaryBufferLength, true);

            var bytes = encoder.GetBytes(new[] { character }, 0, 1, temporary, 0, true);
            WriteBytesInternal(temporary, 0, bytes);
        }

        public void Write(char[] characters)
        {
            Write(characters, 0, characters.Length);
        }

        public void Write(char[] characters, int index, int count)
        {
            Check.NotNull(characters);

            var length = encoding.GetByteCount(characters, index, count);
            RequireLength(length);

            encoding.GetBytes(characters, index, count, buffer.Array, buffer.Offset + position);
            position += length;
        }

        public unsafe void Write(double value)
        {
            var temp = *(ulong*)&value;
            temporary[0] = (byte)temp;
            temporary[1] = (byte)(temp >> 8);
            temporary[2] = (byte)(temp >> 16);
            temporary[3] = (byte)(temp >> 24);
            temporary[4] = (byte)(temp >> 32);
            temporary[5] = (byte)(temp >> 40);
            temporary[6] = (byte)(temp >> 48);
            temporary[7] = (byte)(temp >> 56);

            CopyTemporary(8);
        }

        public void Write(short value)
        {
            temporary[0] = (byte)value;
            temporary[1] = (byte)(value >> 8);

            CopyTemporary(2);
        }

        public void Write(ushort value)
        {
            temporary[0] = (byte)value;
            temporary[1] = (byte)(value >> 8);

            CopyTemporary(2);
        }

        public void Write(int value)
        {
            temporary[0] = (byte)value;
            temporary[1] = (byte)(value >> 8);
            temporary[2] = (byte)(value >> 16);
            temporary[3] = (byte)(value >> 24);

            CopyTemporary(4);
        }

        public void Write(uint value)
        {
            temporary[0] = (byte)value;
            temporary[1] = (byte)(value >> 8);
            temporary[2] = (byte)(value >> 16);
            temporary[3] = (byte)(value >> 24);

            CopyTemporary(4);
        }

        public void Write(long value)
        {
            temporary[0] = (byte)value;
            temporary[1] = (byte)(value >> 8);
            temporary[2] = (byte)(value >> 16);
            temporary[3] = (byte)(value >> 24);
            temporary[4] = (byte)(value >> 32);
            temporary[5] = (byte)(value >> 40);
            temporary[6] = (byte)(value >> 48);
            temporary[7] = (byte)(value >> 56);

            CopyTemporary(8);
        }

        public void Write(ulong value)
        {
            temporary[0] = (byte)value;
            temporary[1] = (byte)(value >> 8);
            temporary[2] = (byte)(value >> 16);
            temporary[3] = (byte)(value >> 24);
            temporary[4] = (byte)(value >> 32);
            temporary[5] = (byte)(value >> 40);
            temporary[6] = (byte)(value >> 48);
            temporary[7] = (byte)(value >> 56);

            CopyTemporary(8);
        }

        public unsafe void Write(float value)
        {
            var temp = *(uint*)&value;
            temporary[0] = (byte)temp;
            temporary[1] = (byte)(temp >> 8);
            temporary[2] = (byte)(temp >> 16);
            temporary[3] = (byte)(temp >> 24);

            CopyTemporary(4);
        }

        public void Write7BitInt32(int value)
        {
            RequireLength(Get7BitEncodedSize(value));

            // write out an int 7 bits at a time. the high bit of the byte, when on, tells reader to continue reading more bytes.
            var v = (uint)value;
            while (v >= 0x80)
            {
                buffer[position++] = (byte)(v | 0x80);
                v >>= 7;
            }

            buffer[position++] = (byte)v;
        }

        public void Write(string value)
        {
            if (value == null)
                throw new ArgumentNullException(nameof(value));

            var count  = value.Length;
            var length = encoding.GetByteCount(value);

            Write7BitInt32(length);
            RequireLength(length);

            encoding.GetBytes(value, 0, count, buffer.Array, buffer.Offset + position);
            position += length;
        }

        public void ModifyAt(int position, Action action)
        {
            var saved = this.position;
            this.position = position;
            action();
            this.position = saved;
        }

        public void ModifyAt(int position, Action<ByteWriter> action)
        {
            var saved = this.position;
            this.position = position;
            action(this);
            this.position = saved;
        }

        public void CopyTo(ByteWriter writer)
        {
            writer.Write(Span);
        }

        public void CopyTo(Stream stream)
        {
            stream.Write(Span);
        }

        public Task CopyToAsync(Stream stream)
        {
            return stream.WriteAsync(Span);
        }

        public Task CopyToAsync(Stream stream, CancellationToken cancellationToken)
        {
            return stream.WriteAsync(Span, cancellationToken);
        }

        public byte[] ToArray()
        {
            return Span.ToArray();
        }

        public byte[] ToArrayAndReturn()
        {
            var array = buffer.ToArray();

            Return();
            return array;
        }
    }
}
