using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using Hina;
using Hina.Collections;
using Hina.Reflection;
using Konseki;
using RtmpSharp.IO.AMF3;
using RtmpSharp.Infos;

namespace RtmpSharp.IO
{
    partial class AmfWriter
    {
        class Amf3
        {
            readonly SerializationContext     context;
            readonly ReferenceList<object>    refObjects;
            readonly ReferenceList<string>    refStrings;
            readonly ReferenceList<ClassInfo> refClasses;

            readonly Base b;
            readonly AmfWriter writer;


            public Amf3(SerializationContext context, AmfWriter writer, Base b)
            {
                this.b       = b;
                this.writer  = writer;
                this.context = context;

                this.refObjects = new ReferenceList<object>();
                this.refStrings = new ReferenceList<string>();
                this.refClasses = new ReferenceList<ClassInfo>();
            }


            // public helper methods

            public void Reset()
            {
                refObjects.Clear();
                refStrings.Clear();
                refClasses.Clear();
            }



            // writers

            public void WriteItem(object value)
            {
                if (value == null)
                {
                    WriteMarker(Marker.Null);
                }
                else
                {
                    var type = value.GetType();

                    if      (Int29Types.Contains(type))                 WriteInt29(Convert.ToInt32(value));
                    else if (NumberTypes.Contains(type))                WriteDouble(Convert.ToDouble(value));
                    else if (Writers.TryGetValue(type, out var writer)) writer(this, value);
                    else                                                DispatchGenericWrite(value);
                }
            }

            void WriteBoolean(bool value)
            {
                WriteMarker(value ? Marker.True : Marker.False);
            }

            void WriteArray(IEnumerable enumerable, int length)
            {
                CheckDebug.NotNull(enumerable);

                WriteMarker(Marker.Array);
                if (ObjectReferenceAddOrWrite(enumerable))
                    return;

                WriteInlineHeaderValue(length);

                // empty key signifies end of associative section
                UnmarkedWriteString("", isString: true);

                foreach (var element in enumerable)
                    WriteItem(element);
            }

            void WriteAssociativeArray(IDictionary<string, object> dictionary)
            {
                CheckDebug.NotNull(dictionary);

                WriteMarker(Marker.Array);
                if (ObjectReferenceAddOrWrite(dictionary))
                    return;

                // inline-header-value: number of dense items - zero for an associative array
                WriteInlineHeaderValue(0);

                foreach (var (key, value) in dictionary)
                {
                    UnmarkedWriteString(key, isString: true);
                    WriteItem(value);
                }

                // empty key signifies end of associative section
                UnmarkedWriteString("", isString: true);
            }

            void WriteByteArray(ArraySegment<byte> value)
            {
                CheckDebug.NotNull(value);

                WriteMarker(Marker.ByteArray);
                if (ObjectReferenceAddOrWrite(value))
                    return;

                // inline-header-value: array length
                WriteInlineHeaderValue(value.Count);
                b.WriteBytes(value.Array, value.Offset, value.Count);
            }

            void WriteByteArray(byte[] value)
            {
                var segment = new ArraySegment<byte>(value);
                WriteByteArray(segment);
            }

            void WriteDateTime(DateTime value)
            {
                WriteMarker(Marker.Date);
                if (ObjectReferenceAddOrWrite(value))
                    return;

                var duration = value.ToUniversalTime() - UnixDateTime.Epoch;
                // not used except to denote inline object
                WriteInlineHeaderValue(0);
                b.WriteDouble(duration.TotalMilliseconds);
            }

            void WriteXDocument(XDocument value)
            {
                CheckDebug.NotNull(value);

                WriteMarker(Marker.Xml);
                if (ObjectReferenceAddOrWrite(value))
                    return;

                UnmarkedWriteString(
                    value.ToString(SaveOptions.DisableFormatting) ?? "",
                    isString: false);
            }

            void WriteXElement(XElement value)
            {
                WriteMarker(Marker.Xml);
                if (ObjectReferenceAddOrWrite(value))
                    return;

                UnmarkedWriteString(
                    value.ToString(SaveOptions.DisableFormatting) ?? "",
                    isString: false);
            }

            void WriteVector<T>(
                Marker    marker,
                bool      isObjectVector,
                bool      isFixedLength,
                IList     items,
                Action<T> write)
            {
                CheckDebug.NotNull(items);

                WriteMarker(marker);
                if (ObjectReferenceAddOrWrite(items))
                    return;

                WriteInlineHeaderValue(items.Count);

                b.WriteBoolean(isFixedLength);
                UnmarkedWriteString(isObjectVector ? "*" : "", isString: true);

                foreach (var item in items)
                    write((T)item);
            }

            void WriteDictionary(IDictionary value)
            {
                CheckDebug.NotNull(value);

                WriteMarker(Marker.Dictionary);
                if (ObjectReferenceAddOrWrite(value))
                    return;

                WriteInlineHeaderValue(value.Count);

                // true:  weakly referenced entries
                // false: strongly referenced entries
                b.WriteBoolean(false);

                foreach (DictionaryEntry entry in value)
                {
                    WriteItem(entry.Key);
                    WriteItem(entry.Value);
                }
            }

            void WriteObject(object obj)
            {
                CheckDebug.NotNull(obj);

                WriteMarker(Marker.Object);
                if (ObjectReferenceAddOrWrite(obj))
                    return;

                var info = context.GetClassInfo(obj);
                if (refClasses.Add(info, out var index))
                {
                    // http://download.macromedia.com/pub/labs/amf/amf3_spec_121207.pdf
                    // """
                    // The first (low) bit is a flag with value 1. The second bit is a flag
                    // (representing whether a trait reference follows) with value 0 to imply that
                    // this objects traits are being sent by reference. The remaining 1 to 27
                    // significant bits are used to encode a trait reference index (an integer).
                    // -- AMF3 specification, 3.12 Object type
                    // """

                    // <u27=trait-reference-index> <0=trait-reference> <1=object-inline>
                    WriteInlineHeaderValue(index << 1);
                }
                else
                {
                    // write the class definition
                    // we can use the same format to serialize normal and extern classes, for simplicity's sake.
                    //     normal:         <u25=member-count>  <u1=dynamic>       <0=externalizable> <1=trait-inline> <1=object-inline>
                    //     externalizable: <u25=insignificant> <u1=insignificant> <1=externalizable> <1=trait-inline> <1=object-inline>
                    var header = info.Members.Length;
                    header = (header << 1) | (info.IsDynamic        ? 1 : 0);
                    header = (header << 1) | (info.IsExternalizable ? 1 : 0);
                    header = (header << 1) | 1;

                    // the final shift is done here.
                    WriteInlineHeaderValue(header);

                    // write the type name
                    UnmarkedWriteString(info.Name, isString: true);

                    // then, write the actual object value
                    if (info.IsExternalizable)
                    {
                        if (!(obj is IExternalizable externalizable))
                            throw new ArgumentException($"{obj.GetType().FullName} ({info.Name}) is marked as externalizable but does not implement IExternalizable");

                        externalizable.WriteExternal(new DataOutput(writer));
                    }
                    else
                    {
                        foreach (var member in info.Members)
                            UnmarkedWriteString(member.Name, isString: true);

                        foreach (var member in info.Members)
                            WriteItem(member.GetValue(obj));

                        if (info.IsDynamic)
                        {
                            if (!(obj is IDictionary<string, object> dictionary))
                                throw new ArgumentException($"{obj.GetType()} is marked as dynamic but does not implement IDictionary");

                            foreach (var (key, value) in dictionary)
                            {
                                UnmarkedWriteString(key, isString: true);
                                WriteItem(value);
                            }

                            UnmarkedWriteString(string.Empty, isString: true);
                        }
                    }
                }
            }

            void WriteVariantNumber(int value)
            {
                if (value >= -268435456 && value <= 268435455)
                    WriteInt29(value);
                else
                    WriteDouble(value);
            }

            void WriteInt29(int value)
            {
                WriteMarker(Marker.Integer);
                UnmarkedWriteInt29(value);
            }

            void WriteDouble(double value)
            {
                WriteMarker(Marker.Double);
                b.WriteDouble(value);
            }

            void WriteString(string value)
            {
                WriteMarker(Marker.String);
                UnmarkedWriteString(value, isString: true);
            }

            // internal helper methods

            void DispatchGenericWrite(object value)
            {
                switch (value)
                {
                    case IExternalizable externalizable:
                        WriteObject(externalizable);
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
                        WriteObject(value);
                        break;
                }
            }

            void UnmarkedWriteString(string value, bool isString)
            {
                CheckDebug.NotNull(value);

                if (value == "")
                {
                    // spec: empty strings are never sent by reference
                    WriteInlineHeaderValue(0);
                    return;
                }

                if (isString ? ReferenceListAddOrWriteInternal(refStrings, value) : ReferenceListAddOrWriteInternal(refObjects, value))
                    return;

                var bytes = Encoding.UTF8.GetBytes(value);
                WriteInlineHeaderValue(bytes.Length);
                b.WriteBytes(bytes);
            }

            // writes a variable length 29-bit signed integer. sign does not matter, may take an unsigned int.
            void UnmarkedWriteInt29(int value)
            {
                Kon.Assert(value >= -268435456 && value <= 268435455, "value isn't in the range of encodable 29-bit numbers");

                // sign contraction - the high order bit of the resulting value must match every bit removed from the number
                // clear 3 bits
                value = value & 0x1fffffff;

                if (value < 0x80)
                {
                    b.WriteByte((byte)value);
                }
                else if (value < 0x4000)
                {
                    b.WriteByte((byte)(value >> 7 & 0x7f | 0x80));
                    b.WriteByte((byte)(value & 0x7f));
                }
                else if (value < 0x200000)
                {
                    b.WriteByte((byte)(value >> 14 & 0x7f | 0x80));
                    b.WriteByte((byte)(value >> 7 & 0x7f | 0x80));
                    b.WriteByte((byte)(value & 0x7f));
                }
                else
                {
                    b.WriteByte((byte)(value >> 22 & 0x7f | 0x80));
                    b.WriteByte((byte)(value >> 15 & 0x7f | 0x80));
                    b.WriteByte((byte)(value >> 8 & 0x7f | 0x80));
                    b.WriteByte((byte)(value & 0xff));
                }
            }

            void WriteMarker(Marker marker)
            {
                b.WriteByte((byte)marker);
            }

            void WriteInlineHeaderValue(int value)
            {
                // 0: object reference
                // 1: inline object (not an object reference)
                UnmarkedWriteInt29((value << 1) | 1);
            }

            // returns true after writing a reference marker if an existing reference existed, otherwise returning false.
            bool ReferenceListAddOrWriteInternal<T>(ReferenceList<T> refs, T value)
            {
                if (refs.Add(value, out var index))
                {
                    // 0: object reference (not inline)
                    // 1: object inline
                    UnmarkedWriteInt29(index << 1);
                    return true;
                }

                return false;
            }

            bool ObjectReferenceAddOrWrite(object value)
            {
                return ReferenceListAddOrWriteInternal(refObjects, value);
            }



            static readonly Type[] Int29Types  = { typeof(SByte), typeof(Byte), typeof(Int16), typeof(UInt16), typeof(Int32), typeof(UInt32) };
            static readonly Type[] NumberTypes = { typeof(Int64), typeof(UInt64), typeof(Single), typeof(Double), typeof(Decimal) };

            // ordering is important, entries here are checked sequentially
            static readonly IDictionary<Type, Action<Amf3, object>> Writers = new KeyDictionary<Type, Action<Amf3, object>>()
            {
                { typeof(bool),      (x, v) => x.WriteBoolean((bool)v)                 },
                { typeof(char),      (x, v) => x.WriteString(v.ToString())             },
                { typeof(string),    (x, v) => x.WriteString((string)v)                },
                { typeof(byte[]),    (x, v) => x.WriteByteArray((byte[])v)             },
                { typeof(DateTime),  (x, v) => x.WriteDateTime((DateTime)v)            },
                { typeof(AsObject),  (x, v) => x.WriteObject((AsObject)v)              }, // required, or asobject will be detected as an IDictionary<string, object> and thus written as an associative array
                { typeof(ByteArray), (x, v) => x.WriteByteArray(((ByteArray)v).Buffer) },
                { typeof(Guid),      (x, v) => x.WriteString(v.ToString())             },
                { typeof(XDocument), (x, v) => x.WriteXDocument((XDocument)v)          },
                { typeof(XElement),  (x, v) => x.WriteXElement((XElement)v)            },
            };

            enum Marker : byte
            {
                Undefined    = 0x00, // 0x00 | 0
                Null         = 0x01, // 0x01 | 1
                False        = 0x02, // 0x02 | 2
                True         = 0x03, // 0x03 | 3
                Integer      = 0x04, // 0x04 | 4
                Double       = 0x05, // 0x05 | 5
                String       = 0x06, // 0x06 | 6
                LegacyXml    = 0x07, // 0x07 | 7
                Date         = 0x08, // 0x08 | 8
                Array        = 0x09, // 0x09 | 9
                Object       = 0x0A, // 0x0A | 10
                Xml          = 0x0B, // 0x0B | 11
                ByteArray    = 0x0C, // 0x0C | 12
                VectorInt    = 0x0D, // 0x0D | 13
                VectorUInt   = 0x0E, // 0x0E | 14
                VectorDouble = 0x0F, // 0x0F | 15
                VectorObject = 0x10, // 0x10 | 16
                Dictionary   = 0x11  // 0x11 | 17
            };
        }
    }
}
