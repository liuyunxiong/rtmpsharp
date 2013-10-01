using System.Xml.Linq;

namespace RtmpSharp.IO.AMF0.AMFWriters
{
    class Amf0XElementWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteMarker(Amf0TypeMarkers.Xml);
            writer.WriteAmf0XElement(obj as XElement);
        }
    }
}
