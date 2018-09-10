using System.Text;
using Hina;
using Hina.IO;

namespace RtmpSharp.IO
{
    partial class AmfWriter
    {
        struct Base
        {
            // `cache` allows us to avoid allocations when reading items that are less than n bytes
            readonly byte[]     temporary;
            readonly ByteWriter writer;


            // constructor

            public Base(ByteWriter writer)
            {
                this.writer    = writer;
                this.temporary = new byte[8];
            }


            // helper

            void CopyTemporary(int length)
                => WriteBytes(temporary, 0, length);


            // writers

            public void WriteByte(byte value)
                => writer.Write(value);

            public void WriteBytes(byte[] buffer)
                => writer.Write(buffer);

            public void WriteBytes(byte[] buffer, int index, int count)
                => writer.Write(buffer, index, count);

            public void WriteBytes(Space<byte> span)
                => writer.Write(span);

            public void WriteInt16(short value)
            {
                temporary[0] = (byte)(value >> 8);
                temporary[1] = (byte)value;

                CopyTemporary(2);
            }

            public void WriteUInt16(ushort value)
            {
                temporary[0] = (byte)(value >> 8);
                temporary[1] = (byte)value;

                CopyTemporary(2);
            }

            public void WriteInt32(int value)
            {
                temporary[0] = (byte)(value >> 24);
                temporary[1] = (byte)(value >> 16);
                temporary[2] = (byte)(value >> 8);
                temporary[3] = (byte)value;

                CopyTemporary(4);
            }

            public void WriteUInt32(uint value)
            {
                temporary[0] = (byte)(value >> 24);
                temporary[1] = (byte)(value >> 16);
                temporary[2] = (byte)(value >> 8);
                temporary[3] = (byte)value;

                CopyTemporary(4);
            }

            // writes a little endian 32-bit integer
            public void WriteLittleEndianInt(uint value)
            {
                temporary[0] = (byte)value;
                temporary[1] = (byte)(value >> 8);
                temporary[2] = (byte)(value >> 16);
                temporary[3] = (byte)(value >> 24);

                CopyTemporary(4);
            }

            public void WriteUInt24(uint value)
            {
                temporary[0] = (byte)(value >> 16);
                temporary[1] = (byte)(value >> 8);
                temporary[2] = (byte)(value >> 0);

                CopyTemporary(3);
            }

            public void WriteBoolean(bool value)
            {
                WriteByte(value ? (byte)1 : (byte)0);
            }

            public unsafe void WriteDouble(double value)
            {
                var temp = *((ulong*)&value);

                temporary[0] = (byte)(temp >> 56);
                temporary[1] = (byte)(temp >> 48);
                temporary[2] = (byte)(temp >> 40);
                temporary[3] = (byte)(temp >> 32);
                temporary[4] = (byte)(temp >> 24);
                temporary[5] = (byte)(temp >> 16);
                temporary[6] = (byte)(temp >> 8);
                temporary[7] = (byte)temp;

                CopyTemporary(8);
            }

            public unsafe void WriteSingle(float value)
            {
                var temp = *((uint*)&value);

                temporary[0] = (byte)(temp >> 24);
                temporary[1] = (byte)(temp >> 16);
                temporary[2] = (byte)(temp >> 8);
                temporary[3] = (byte)temp;

                CopyTemporary(4);
            }

            // string with 16-bit length prefix
            public void WriteUtfPrefixed(string value)
            {
                Check.NotNull(value);

                var utf8 = Encoding.UTF8.GetBytes(value);
                WriteUtfPrefixed(utf8);
            }

            public void WriteUtfPrefixed(byte[] utf8)
            {
                Check.NotNull(utf8);

                WriteUInt16((ushort)utf8.Length);
                WriteBytes(utf8);
            }
        }
    }
}
