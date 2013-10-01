using System;

namespace RtmpSharp.IO.AMF0.AMFWriters
{
    class Amf0NumberWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteMarker(Amf0TypeMarkers.Number);
            writer.WriteDouble(Convert.ToDouble(obj));
        }
    }
}
