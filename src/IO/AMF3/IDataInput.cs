using Hina;

namespace RtmpSharp.IO.AMF3
{
    public interface IDataInput
    {
        object      ReadObject();
        bool        ReadBoolean();
        byte        ReadByte();
        byte[]      ReadBytes(int count);
        void        ReadBytes(byte[] buffer, int index, int count);
        Space<byte> ReadSpan(int count);
        double      ReadDouble();
        float       ReadSingle();
        short       ReadInt16();
        ushort      ReadUInt16();
        uint        ReadUInt24();
        int         ReadInt32();
        uint        ReadUInt32();
        string      ReadUtf();
        string      ReadUtf(int length);

    }
}