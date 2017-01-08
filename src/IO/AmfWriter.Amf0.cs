using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Hina;
using Hina.Collections;
using Hina.Reflection;

namespace RtmpSharp.IO
{
    partial class AmfWriter
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


            // helper methods

            public void Reset() => refs.Clear();

            void ReferenceAdd(object value) => refs.Add(value, out var _);
            bool ReferenceAdd(object value, out ushort index) => refs.Add(value, out index);
            bool ReferenceGet(object value, out ushort index) => refs.TryGetValue(value, out index);


            // writers


            public void WriteItem(object value)
            {
                if (value == null)
                {
                    WriteMarker(Marker.Null);
                }
                else if (ReferenceGet(value, out var index))
                {
                    WriteMarker(Marker.Reference);
                    b.WriteUInt16(index);
                }
                else
                {
                    WriteItemInternal(value);
                }
            }

            // writes an object, with the specified encoding. if amf3 encoding is specified, then it is wrapped in an
            // amf0 envelope that says to upgrade the encoding to amf3
            public void WriteBoxedItem(ObjectEncoding encoding, object value)
            {
                if (value == null)
                {
                    WriteMarker(Marker.Null);
                }
                else if (ReferenceGet(value, out var index))
                {
                    WriteMarker(Marker.Reference);
                    b.WriteUInt16(index);
                }
                else
                {
                    switch (encoding)
                    {
                        case ObjectEncoding.Amf0:
                            WriteItemInternal(value);
                            break;

                        case ObjectEncoding.Amf3:
                            WriteMarker(Marker.Amf3Object);
                            amf3.WriteItem(value);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(encoding));
                    }
                }
            }

            void WriteItemInternal(object value)
            {
                var type = value.GetType();

                if      (NumberTypes.Contains(type))               WriteNumber(Convert.ToDouble(value));
                else if (Writers.TryGetValue(type, out var write)) write(this, value);
                else                                               DispatchGenericWrite(value);
            }

            // writes a string, either as a short or long strong depending on length.
            void WriteVariantString(string value)
            {
                CheckDebug.NotNull(value);

                var utf8   = Encoding.UTF8.GetBytes(value);
                var length = utf8.Length;

                if (length < ushort.MaxValue)
                {
                    // unsigned 16-bit length
                    WriteMarker(Marker.String);
                    b.WriteUInt16((ushort)utf8.Length);
                    b.WriteBytes(utf8);
                }
                else
                {
                    // unsigned 32-bit length
                    WriteMarker(Marker.LongString);
                    b.WriteUInt32((uint)utf8.Length);
                    b.WriteBytes(utf8);
                }
            }

            void WriteAsObject(AsObject value)
            {
                CheckDebug.NotNull(value);
                ReferenceAdd(value);

                if (string.IsNullOrEmpty(value.TypeName))
                {
                    WriteMarker(Marker.Object);
                }
                else
                {
                    WriteMarker(Marker.TypedObject);
                    b.WriteUtfPrefixed(value.TypeName);
                }

                foreach (var property in value)
                {
                    b.WriteUtfPrefixed(property.Key);
                    WriteItem(property.Value);
                }

                // object end is marked with a zero-length field name, and an end of object marker.
                b.WriteUInt16(0);
                WriteMarker(Marker.ObjectEnd);
            }

            void WriteTypedObject(object value)
            {
                CheckDebug.NotNull(value);
                ReferenceAdd(value);

                var klass = context.GetClassInfo(value);

                WriteMarker(Marker.TypedObject);
                b.WriteUtfPrefixed(klass.Name);

                foreach (var member in klass.Members)
                {
                    b.WriteUtfPrefixed(member.Name);
                    WriteItem(member.GetValue(value));
                }

                // object end is marked with a zero-length field name, and an end of object marker.
                b.WriteUInt16(0);
                WriteMarker(Marker.ObjectEnd);
            }

            void WriteDateTime(DateTime value)
            {
                // http://download.macromedia.com/pub/labs/amf/amf0_spec_121207.pdf
                // """
                // While the design of this type reserves room for time zone offset information,
                // it should not be filled in, nor used, as it is unconventional to change time
                // zones when serializing dates on a network. It is suggested that the time zone
                // be queried independently as needed.
                //  -- AMF0 specification, 2.13 Date Type
                // """

                var duration = value.ToUniversalTime() - UnixDateTime.Epoch;
                WriteMarker(Marker.Date);
                b.WriteDouble(duration.TotalMilliseconds);
                b.WriteUInt16(0); // time zone offset
            }

            void WriteXDocument(XDocument value)
            {
                CheckDebug.NotNull(value);
                ReferenceAdd(value);

                UnmarkedWriteLongString(
                    value.ToString(SaveOptions.DisableFormatting));
            }

            void WriteXElement(XElement value)
            {
                CheckDebug.NotNull(value);
                ReferenceAdd(value);

                UnmarkedWriteLongString(
                    value.ToString(SaveOptions.DisableFormatting));
            }

            void WriteArray(IEnumerable enumerable, int length)
            {
                CheckDebug.NotNull(enumerable);
                ReferenceAdd(enumerable);

                b.WriteInt32(length);

                foreach (var element in enumerable)
                    WriteItem(element);
            }

            void WriteAssociativeArray(IDictionary<string, object> dictionary)
            {
                CheckDebug.NotNull(dictionary);
                ReferenceAdd(dictionary);

                WriteMarker(Marker.EcmaArray);
                b.WriteInt32(dictionary.Count);

                foreach (var (key, value) in dictionary)
                {
                    b.WriteUtfPrefixed(key);
                    WriteItem(value);
                }

                // object end is marked with a zero-length field name, and an end of object marker.
                b.WriteUInt16(0);
                WriteMarker(Marker.ObjectEnd);
            }

            void WriteBoolean(bool value)
            {
                WriteMarker(Marker.Boolean);
                b.WriteBoolean(value);
            }

            void WriteNumber(double value)
            {
                WriteMarker(Marker.Number);
                b.WriteDouble(value);
            }


            void DispatchGenericWrite(object value)
            {
                switch (value)
                {
                    case Enum e:
                        WriteNumber(Convert.ToDouble(e));
                        break;

                    case IDictionary<string, object> dictionary:
                        WriteAssociativeArray(dictionary);
                        break;

                    case IList list:
                        WriteArray(list, list.Count);
                        break;

                    case ICollection collection:
                        WriteArray(collection, collection.Count);
                        break;

                    case IEnumerable enumerable:
                        var type = value.GetType();

                        if (type.ImplementsGenericInterface(typeof(ICollection<>)) || type.ImplementsGenericInterface(typeof(IList<>)))
                        {
                            dynamic d = value;
                            int count = d.Count;

                            WriteArray(enumerable, count);
                        }
                        else
                        {
                            var values = enumerable.Cast<object>().ToArray();
                            WriteArray(values, values.Length);
                        }

                        break;

                    default:
                        WriteTypedObject(value);
                        break;
                }
            }

            void UnmarkedWriteLongString(string value)
            {
                CheckDebug.NotNull(value);

                var utf8 = Encoding.UTF8.GetBytes(value);

                WriteMarker(Marker.LongString);
                b.WriteUInt32((uint)utf8.Length);
                b.WriteBytes(utf8);
            }

            void WriteMarker(Marker marker)
            {
                b.WriteByte((byte)marker);
            }


            static readonly Type[] NumberTypes =
            {
                typeof(Byte),
                typeof(Int16),
                typeof(Int32),
                typeof(Int64),

                typeof(SByte),
                typeof(UInt16),
                typeof(UInt32),
                typeof(UInt64),

                typeof(Single),
                typeof(Double),
                typeof(Decimal)
            };

            // ordering is important, entries here are checked sequentially
            static readonly IDictionary<Type, Action<Amf0, object>> Writers = new KeyDictionary<Type, Action<Amf0, object>>()
            {
                { typeof(bool),      (x, v) => x.WriteBoolean((bool)v)            },
                { typeof(char),      (x, v) => x.WriteVariantString(v.ToString()) },
                { typeof(string),    (x, v) => x.WriteVariantString((string)v)    },
                { typeof(DateTime),  (x, v) => x.WriteDateTime((DateTime)v)       },
                { typeof(AsObject),  (x, v) => x.WriteAsObject((AsObject)v)       },
                { typeof(Guid),      (x, v) => x.WriteVariantString(v.ToString()) },
                { typeof(XDocument), (x, v) => x.WriteXDocument((XDocument)v)     },
                { typeof(XElement),  (x, v) => x.WriteXElement((XElement)v)       },
            };

            enum Marker : byte
            {
                Number      = 0x00, // 0x00 | 0
                Boolean     = 0x01, // 0x01 | 1
                String      = 0x02, // 0x02 | 2
                Object      = 0x03, // 0x03 | 3
                Movieclip   = 0x04, // 0x04 | 4
                Null        = 0x05, // 0x05 | 5
                Undefined   = 0x06, // 0x06 | 6
                Reference   = 0x07, // 0x07 | 7
                EcmaArray   = 0x08, // 0x08 | 8
                ObjectEnd   = 0x09, // 0x09 | 9
                StrictArray = 0x0A, // 0x0A | 10
                Date        = 0x0B, // 0x0B | 11
                LongString  = 0x0C, // 0x0C | 12
                Unsupported = 0x0D, // 0x0D | 13
                Recordset   = 0x0E, // 0x0E | 14
                Xml         = 0x0F, // 0x0F | 15
                TypedObject = 0x10, // 0x10 | 16
                Amf3Object  = 0x11, // 0x11 | 17
            };
        }
    }
}
