using System;

namespace RtmpSharp.IO.AMF3.AMFWriters
{
    class Amf3ArrayWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteMarker(Amf3TypeMarkers.Array);
            writer.WriteAmf3Array(obj as Array);
        }
    }
}