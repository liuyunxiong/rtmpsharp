using System;
using Hina;

namespace RtmpSharp.IO.AMF3
{
    class DataInput : IDataInput
    {
        ObjectEncoding encoding;
        readonly AmfReader reader;

        public DataInput(AmfReader reader)
        {
            this.reader   = reader;
            this.encoding = ObjectEncoding.Amf3;
        }

        public ObjectEncoding ObjectEncoding
        {
            get => encoding;
            set => encoding = value;
        }

        public object ReadObject()
        {
            switch (encoding)
            {
                case ObjectEncoding.Amf0: return reader.ReadAmf0Object();
                case ObjectEncoding.Amf3: return reader.ReadAmf3Object();
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public bool   ReadBoolean()        => reader.ReadBoolean();
        public byte   ReadByte()           => reader.ReadByte();
        public byte[] ReadBytes(int count) => reader.ReadBytes(count);
        public double ReadDouble()         => reader.ReadDouble();
        public float  ReadSingle()         => reader.ReadSingle();
        public short  ReadInt16()          => reader.ReadInt16();
        public ushort ReadUInt16()         => reader.ReadUInt16();
        public uint   ReadUInt24()         => reader.ReadUInt24();
        public int    ReadInt32()          => reader.ReadInt32();
        public uint   ReadUInt32()         => reader.ReadUInt32();
        public string ReadUtf()            => reader.ReadUtf();
        public string ReadUtf(int length)  => reader.ReadUtf(length);

        public Space<byte> ReadSpan(int count) => reader.ReadSpan(count);
        public void        ReadBytes(byte[] buffer, int index, int count) => reader.ReadBytes(buffer, index, count);
    }
}
