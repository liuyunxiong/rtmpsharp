using System.Xml.Linq;

namespace RtmpSharp.IO.AMF3.AMFWriters
{
    class Amf3XElementWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteMarker(Amf3TypeMarkers.Xml);
            writer.WriteAmf3XElement(obj as XElement);
        }
    }
}