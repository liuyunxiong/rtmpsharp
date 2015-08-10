using System;

namespace RtmpSharp.IO.AMF3
{
    class DataInput : IDataInput
    {
        private readonly AmfReader reader;
        private ObjectEncoding objectEncoding;

        public DataInput(AmfReader reader)
        {
            this.reader = reader;
            this.objectEncoding = ObjectEncoding.Amf3;
        }

        public ObjectEncoding ObjectEncoding
        {
            get { return objectEncoding; }
            set { objectEncoding = value; }
        }

        public object ReadObject()
        {
            switch (objectEncoding)
            {
                case ObjectEncoding.Amf0:
                    return reader.ReadAmf0Item();
                case ObjectEncoding.Amf3:
                    return reader.ReadAmf3Item();
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public bool ReadBoolean() => reader.ReadBoolean();
        public byte ReadByte() => reader.ReadByte();
        public byte[] ReadBytes(int count) => reader.ReadBytes(count);
        public double ReadDouble() => reader.ReadDouble();
        public float ReadFloat() => reader.ReadFloat();
        public short ReadInt16() => reader.ReadInt16();
        public ushort ReadUInt16() => reader.ReadUInt16();
        public int ReadUInt24() => reader.ReadUInt24();
        public int ReadInt32() => reader.ReadInt32();
        public uint ReadUInt32() => reader.ReadUInt32();
        public string ReadUtf() => reader.ReadUtf();
        public string ReadUtf(int length) => reader.ReadUtf(length);
    }
}
