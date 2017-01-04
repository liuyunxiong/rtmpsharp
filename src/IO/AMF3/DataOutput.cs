using System;
using System.Text;
using Hina;

namespace RtmpSharp.IO.AMF3
{
    class DataOutput : IDataOutput
    {
        ObjectEncoding encoding;
        readonly AmfWriter writer;

        public DataOutput(AmfWriter writer)
        {
            this.writer   = writer;
            this.encoding = ObjectEncoding.Amf3;
        }

        public ObjectEncoding ObjectEncoding
        {
            get => encoding;
            set => encoding = value;
        }
        
        public void WriteObject(object value)
        {
            switch (encoding)
            {
                case ObjectEncoding.Amf0: writer.WriteAmf0Object(value); break;
                case ObjectEncoding.Amf3: writer.WriteAmf3Object(value); break;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public void WriteBoolean(bool value)     => writer.WriteBoolean(value);
        public void WriteUInt32(uint value)      => writer.WriteUInt32(value);
        public void WriteByte(byte value)        => writer.WriteByte(value);
        public void WriteBytes(byte[] buffer)    => writer.WriteBytes(buffer);
        public void WriteBytes(Space<byte> span) => writer.WriteBytes(span);
        public void WriteDouble(double value)    => writer.WriteDouble(value);
        public void WriteSingle(float value)     => writer.WriteSingle(value);
        public void WriteInt16(short value)      => writer.WriteInt16(value);
        public void WriteInt32(int value)        => writer.WriteInt32(value);
        public void WriteUInt16(ushort value)    => writer.WriteUInt16(value);
        public void WriteUInt24(uint value)      => writer.WriteUInt24(value);
        public void WriteUtf(string value)       => writer.WriteUtfPrefixed(value);
        public void WriteUtfBytes(string value)  => writer.WriteBytes(Encoding.UTF8.GetBytes(value));

        public void WriteBytes(byte[] buffer, int index, int count) => writer.WriteBytes(buffer, index, count);
    }
}
