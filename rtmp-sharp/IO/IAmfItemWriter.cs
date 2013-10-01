using System;

namespace RtmpSharp.IO
{
    interface IAmfItemWriter
    {
        void WriteData(AmfWriter writer, Object obj);
    }
}
