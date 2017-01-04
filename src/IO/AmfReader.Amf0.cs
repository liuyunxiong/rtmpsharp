using System;
using System.Collections.Generic;
using System.Xml.Linq;
using Hina;

namespace RtmpSharp.IO
{
    partial class AmfReader
    {
        class Amf0
        {
            readonly SerializationContext context;
            readonly ReferenceList<object> refs;

            readonly Base b;
            readonly Amf3 amf3;

            public Amf0(SerializationContext context, Base b, Amf3 amf3)
            {
                this.b       = b;
                this.amf3    = amf3;
                this.context = context;
                this.refs    = new ReferenceList<object>();
            }


            // public helper methods

            public void Reset() => refs.Clear();
            public bool HasLength(int count) => b.HasLength(count);


            // read_* implementations

            public object ReadItem()
            {
                var marker = b.ReadByte();
                return ReadItem(marker);
            }

            object ReadItem(int marker)
            {
                return readers[marker](b, this, amf3);
            }

            object ReadObjectRef()
            {
                return refs.Get(b.ReadUInt16());
            }

            // amf0 object
            object ReadObject()
            {
                var type = b.ReadUtf();

                if (context.HasConcreteType(type))
                {
                    var instance = context.CreateInstance(type);
                    var klass    = context.GetClassInfo(instance);

                    refs.Add(instance);

                    foreach (var pair in ReadItems())
                    {
                        if (klass.TryGetMember(pair.key, out var member))
                            member.SetValue(instance, pair.value);
                    }

                    return instance;
                }
                else if (context.AsObjectFallback)
                {
                    // object reference added in this.ReadAmf0AsObject()
                    var obj = ReadAsObject();

                    obj.TypeName = type;
                    return obj;
                }
                else
                {
                    throw new ArgumentException($"can't deserialize object: the type \"{type}\" isn't registered, and anonymous object fallback has been disabled");
                }
            }

            AsObject ReadAsObject()
            {
                var asObject = new AsObject();

                refs.Add(asObject);
                asObject.Replace(ReadItems());

                return asObject;
            }

            string ReadLongString()
            {
                var length = b.ReadInt32();
                return b.ReadUtf(length);
            }

            Dictionary<string, object> ReadEcmaArray()
            {
                var length     = b.ReadInt32();
                var dictionary = new Dictionary<string, object>(length);
                refs.Add(dictionary);

                foreach (var (key, value) in ReadItems())
                    dictionary[key] = value;

                return dictionary;
            }

            object[] ReadStrictArray()
            {
                var length = b.ReadInt32();
                var array  = new object[length];

                refs.Add(array);

                for (var i = 0; i < length; i++)
                    array[i] = ReadItem();

                return array;
            }

            DateTime ReadDate()
            {
                var milliseconds = b.ReadDouble();
                var date         = UnixDateTime.Epoch.AddMilliseconds(milliseconds);

                // http://download.macromedia.com/pub/labs/amf/amf0_spec_121207.pdf
                // """
                // While the design of this type reserves room for time zone offset information,
                // it should not be filled in, nor used, as it is unconventional to change time
                // zones when serializing dates on a network. It is suggested that the time zone
                // be queried independently as needed.
                //  -- AMF0 specification, 2.13 Date Type
                // """
                var offset = b.ReadUInt16();

                return date;
            }

            XDocument ReadXmlDocument()
            {
                var xml = ReadLongString();

                return string.IsNullOrEmpty(xml)
                    ? new XDocument()
                    : XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
            }


            // internal helper methods

            List<(string key, object value)> ReadItems()
            {
                var pairs = new List<(string key, object value)>();

                while (true)
                {
                    var key  = b.ReadUtf();
                    var type = b.ReadByte();

                    if (type == (byte)0x09) // object-end marker
                        return pairs;

                    pairs.Add((key: key, value: ReadItem(type)));
                }
            }


            static readonly List<Func<Base, Amf0, Amf3, object>> readers = new List<Func<Base, Amf0, Amf3, object>>
            {
                (core, amf0, amf3) => core.ReadDouble(),                      // 0x00 - number
                (core, amf0, amf3) => core.ReadBoolean(),                     // 0x01 - boolean
                (core, amf0, amf3) => core.ReadUtf(),                         // 0x02 - string
                (core, amf0, amf3) => amf0.ReadAsObject(),                    // 0x03 - object
                (core, amf0, amf3) => { throw new NotSupportedException(); }, // 0x04 - movieclip
                (core, amf0, amf3) => null,                                   // 0x05 - null
                (core, amf0, amf3) => null,                                   // 0x06 - undefined
                (core, amf0, amf3) => amf0.ReadObjectRef(),                   // 0x07 - reference
                (core, amf0, amf3) => amf0.ReadEcmaArray(),                   // 0x08 - ECMA array
                (core, amf0, amf3) => { throw new NotSupportedException(); }, // 0x09 - 'object end marker' - we handle this in deserializer block; we shouldn't encounter it here
                (core, amf0, amf3) => amf0.ReadStrictArray(),                 // 0x0A - strict array
                (core, amf0, amf3) => amf0.ReadDate(),                        // 0x0B - date
                (core, amf0, amf3) => amf0.ReadLongString(),                  // 0x0C - long string
                (core, amf0, amf3) => { throw new NotSupportedException(); }, // 0x0D - unsupported marker
                (core, amf0, amf3) => { throw new NotSupportedException(); }, // 0x0E - recordset
                (core, amf0, amf3) => amf0.ReadXmlDocument(),                 // 0x0F - xml document
                (core, amf0, amf3) => amf0.ReadObject(),                      // 0x10 - typed object
                (core, amf0, amf3) => amf3.ReadItem()                         // 0x11 - avmplus object
            };
        }
    }
}
