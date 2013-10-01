
namespace RtmpSharp.IO.AMF0.AMFWriters
{
    class Amf0CharWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteMarker(Amf0TypeMarkers.String);
            writer.WriteUtfPrefixed(obj.ToString());
        }
    }
}
