
namespace RtmpSharp.IO.AMF3.AMFWriters
{
    // If we don't have this here, it'll be written as an IDictionary<string, object> (associative array). We don't want that.
    class Amf3AsObjectWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteMarker(Amf3TypeMarkers.Object);
            writer.WriteAmf3Object(obj);
        }
    }
}