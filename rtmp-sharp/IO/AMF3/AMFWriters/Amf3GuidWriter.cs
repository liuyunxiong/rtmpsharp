using System;

namespace RtmpSharp.IO.AMF3.AMFWriters
{
    class Amf3GuidWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteMarker(Amf3TypeMarkers.String);
            writer.WriteAmf3Utf(((Guid)obj).ToString());
        }
    }
}