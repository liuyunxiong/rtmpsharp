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

        public string ReadUtf()
        {
            return reader.ReadUtf();
        }

        public string ReadUtf(int length)
        {
            return reader.ReadUtf(length);
        }

        public int ReadUInt24()
        {
            return reader.ReadUInt24();
        }

        public ushort ReadUInt16()
        {
            return reader.ReadUInt16();
        }

        public int ReadInt32()
        {
            return reader.ReadInt32();
        }

        public uint ReadUInt32()
        {
            return reader.ReadUInt32();
        }

        public short ReadInt16()
        {
            return reader.ReadInt16();
        }

        public float ReadFloat()
        {
            return reader.ReadFloat();
        }

        public double ReadDouble()
        {
            return reader.ReadDouble();
        }

        public byte[] ReadBytes(int count)
        {
            return reader.ReadBytes(count);
        }

        public byte ReadByte()
        {
            return reader.ReadByte();
        }

        public bool ReadBoolean()
        {
            return reader.ReadBoolean();
        }
    }
}
