
namespace RtmpSharp.IO.AMF3
{
    public interface IDataInput
    {
        object ReadObject();
        bool ReadBoolean();
        byte ReadByte();
        byte[] ReadBytes(int count);
        double ReadDouble();
        float ReadFloat();
        short ReadInt16();
        ushort ReadUInt16();
        int ReadUInt24();
        int ReadInt32();
        uint ReadUInt32();
        string ReadUtf();
        string ReadUtf(int length);
        
    }
}