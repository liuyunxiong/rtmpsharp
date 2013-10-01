
namespace RtmpSharp.IO.AMF0.AMFWriters
{
    class Amf0BooleanWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteMarker(Amf0TypeMarkers.Boolean);
            writer.WriteBoolean((bool)obj);
        }
    }
}
