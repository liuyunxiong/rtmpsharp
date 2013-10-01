using System;
using System.Text;

namespace RtmpSharp.IO.AMF3
{
    class DataOutput : IDataOutput
    {
        private AmfWriter writer;
        private ObjectEncoding objectEncoding;

        public DataOutput(AmfWriter writer)
        {
            this.writer = writer;
            this.objectEncoding = ObjectEncoding.Amf3;
        }

        public ObjectEncoding ObjectEncoding
        {
            get { return objectEncoding; }
            set { objectEncoding = value; }
        }
        
        public void WriteObject(object value)
        {
            switch (objectEncoding)
            {
                case ObjectEncoding.Amf0:
                    writer.WriteAmfItem(ObjectEncoding.Amf0, value);
                    break;
                case ObjectEncoding.Amf3:
                    writer.WriteAmf3Item(value);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        public void WriteBoolean(bool value)
        {
            writer.WriteBoolean(value);
        }

        public void WriteUInt32(uint value)
        {
            writer.WriteUInt32(value);
        }

        public void WriteByte(byte value)
        {
            writer.WriteByte(value);
        }

        public void WriteBytes(byte[] buffer)
        {
            writer.WriteBytes(buffer);
        }

        public void WriteDouble(double value)
        {
            writer.WriteDouble(value);
        }

        public void WriteFloat(float value)
        {
            writer.WriteFloat(value);
        }

        public void WriteInt16(short value)
        {
            writer.WriteInt16(value);
        }

        public void WriteInt32(int value)
        {
            writer.WriteInt32(value);
        }

        public void WriteUInt16(ushort value)
        {
            writer.WriteUInt16(value);
        }

        public void WriteUInt24(int value)
        {
            writer.WriteUInt24(value);
        }

        public void WriteUtf(string value)
        {
            writer.WriteUtfPrefixed(value);
        }

        public void WriteUtfBytes(string value)
        {
            writer.WriteBytes(Encoding.UTF8.GetBytes(value));
        }
    }
}
