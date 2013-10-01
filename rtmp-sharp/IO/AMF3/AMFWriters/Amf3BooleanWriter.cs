
namespace RtmpSharp.IO.AMF3.AMFWriters
{
    class Amf3BooleanWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteAmf3BoolSpecial((bool)obj);
        }
    }
}