using System;
using System.IO;
using System.Text;
using Hina;
using Hina.IO;
using Konseki;

namespace RtmpSharp.IO
{
    partial class AmfReader
    {
        class Base
        {
            // `cache` allows us to avoid allocations when reading items that are less than n bytes
            readonly byte[]     temporary;
            readonly ByteReader reader;


            // constructor

            public Base(ByteReader reader)
            {
                this.reader    = reader;
                this.temporary = new byte[8];
            }


            // helpers

            void Require(int count)
            {
                Kon.Assert(count <= temporary.Length);

                if (reader.ReadUnchecked(temporary, 0, count) != count)
                    throw new EndOfStreamException();
            }

            public bool HasLength(int count)
            {
                return reader.HasLength(count);
            }


            // readers

            public byte ReadByte()
            {
                return reader.ReadByte();
            }

            public byte[] ReadBytes(int count)
            {
                return reader.ReadBytes(count);
            }

            public void ReadBytes(byte[] buffer, int index, int count)
            {
                if (reader.Read(buffer, index, count) != count)
                    throw new ArgumentOutOfRangeException("tried to read past end of data stream");
            }

            public Space<byte> ReadSpan(int count)
            {
                return reader.ReadSpan(count);
            }

            public ushort ReadUInt16()
            {
                Require(2);
                return (ushort)(((temporary[0] & 0xFF) << 8) | (temporary[1] & 0xFF));
            }

            public short ReadInt16()
            {
                Require(2);
                return (short)(((temporary[0] & 0xFF) << 8) | (temporary[1] & 0xFF));
            }

            public bool ReadBoolean()
            {
                return reader.ReadBoolean();
            }

            public int ReadInt32()
            {
                Require(4);
                return (int)(temporary[0] << 24) | (temporary[1] << 16) | (temporary[2] << 8) | temporary[3];
            }

            public uint ReadUInt32()
            {
                Require(4);
                return (uint)((temporary[0] << 24) | (temporary[1] << 16) | (temporary[2] << 8) | temporary[3]);
            }

            public int ReadLittleEndianInt()
            {
                Require(4);
                return (temporary[3] << 24) | (temporary[2] << 16) | (temporary[1] << 8) | temporary[0];
            }

            public uint ReadUInt24()
            {
                Require(3);
                return (uint)(temporary[0] << 16 | temporary[1] << 8 | temporary[2]);
            }

            // 64-bit IEEE-754 double precision floating point
            public unsafe double ReadDouble()
            {
                Require(8);
                var lo = (uint)(temporary[7] | temporary[6] << 8 | temporary[5] << 16 | temporary[4] << 24);
                var hi = (uint)(temporary[3] | temporary[2] << 8 | temporary[1] << 16 | temporary[0] << 24);
                var value = (ulong)hi << 32 | lo;
                return *(double*)&value;
            }

            // single-precision floating point number
            public unsafe float ReadSingle()
            {
                Require(4);
                var value = (uint)(temporary[0] << 24 | temporary[2] << 8 | temporary[1] << 16 | temporary[3]);
                return *(float*)&value;
            }

            // utf8 string with length prefix
            public string ReadUtf()
            {
                var length = ReadUInt16();
                return ReadUtf(length);
            }

            // utf8 string
            public string ReadUtf(int length)
            {
                if (length == 0)
                    return string.Empty;

                return Encoding.UTF8.GetString(
                    bytes: ReadBytes(length),
                    index: 0,
                    count: length);
            }
        }
    }
}