using System;

namespace RtmpSharp.IO.AMF0.AMFWriters
{
    class Amf0ArrayWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteMarker(Amf0TypeMarkers.StrictArray);
            writer.WriteAmf0Array(obj as Array);
        }
    }
}
