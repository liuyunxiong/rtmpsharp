
namespace RtmpSharp.IO.AMF3
{
    public interface IExternalizable
    {
        void ReadExternal(IDataInput input);
        void WriteExternal(IDataOutput output);
    }
}
