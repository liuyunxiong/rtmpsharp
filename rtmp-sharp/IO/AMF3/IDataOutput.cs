
namespace RtmpSharp.IO.AMF3
{
    public interface IDataOutput
    {
        void WriteObject(object value);
        void WriteBoolean(bool value);
        void WriteByte(byte value);
        void WriteBytes(byte[] buffer);
        void WriteDouble(double value);
        void WriteFloat(float value);
        void WriteInt16(short value);
        void WriteUInt16(ushort value);
        void WriteUInt24(int value);
        void WriteInt32(int value);
        void WriteUInt32(uint value);
        void WriteUtf(string value);
        // writes string without 16-bit length prefix
        void WriteUtfBytes(string value);
    }
}