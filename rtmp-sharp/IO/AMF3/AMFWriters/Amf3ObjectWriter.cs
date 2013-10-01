using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace RtmpSharp.IO.AMF3.AMFWriters
{
    class Amf3ObjectWriter : IAmfItemWriter
    {
        public void WriteData(AmfWriter writer, object obj)
        {
            var externalizable = obj is IExternalizable;

            // if IExternalizable then use those methods, even if it is a collection
            if (!externalizable)
            {
                IDictionary<string, object> stringDictionary;
                IDictionary dictionary;
                IEnumerable enumerable;

                if ((stringDictionary = obj as IDictionary<string, object>) != null)
                {
                    writer.WriteMarker(Amf3TypeMarkers.Array);
                    writer.WriteAmf3AssociativeArray(stringDictionary);
                    return;
                }
                if ((dictionary = obj as IDictionary) != null)
                {
                    writer.WriteMarker(Amf3TypeMarkers.Dictionary);
                    writer.WriteAmf3Dictionary(dictionary);
                }
                if ((enumerable = obj as IEnumerable) != null)
                {
                    writer.WriteMarker(Amf3TypeMarkers.Array);
                    writer.WriteAmf3Array(enumerable.Cast<object>().ToArray());
                    return;
                }
            }

            writer.WriteMarker(Amf3TypeMarkers.Object);
            writer.WriteAmf3Object(obj);
        }
    }
}