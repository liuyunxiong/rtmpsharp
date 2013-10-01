using System;

namespace RtmpSharp.IO.AMF3.AMFWriters
{
    class Amf3IntWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            writer.WriteAmf3NumberSpecial(Convert.ToInt32(obj));
        }
    }
}