using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RtmpSharp.IO.AMF0.AMFWriters
{
    class Amf0ObjectWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            IDictionary<string, object> dictionary;
            IEnumerable enumerable;

            if ((dictionary = obj as IDictionary<string, object>) != null)
            {
                // method writes type marker
                writer.WriteAmf0AssociativeArray(dictionary);
            }
            else if ((enumerable = obj as IEnumerable) != null)
            {
                writer.WriteMarker(Amf0TypeMarkers.StrictArray);
                writer.WriteAmf0Array(enumerable.Cast<object>().ToArray());
            }
            else
            {
                // method writes type marker
                writer.WriteAmf0TypedObject(obj);
            }
        }
    }
}
