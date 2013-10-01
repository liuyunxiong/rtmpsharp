
namespace RtmpSharp.IO.AMF3.AMFWriters
{
    class Amf3ByteArrayWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteMarker(Amf3TypeMarkers.ByteArray);
            writer.WriteAmf3ByteArray(obj as ByteArray);
        }
    }
}