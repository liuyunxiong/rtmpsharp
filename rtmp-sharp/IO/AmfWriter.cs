using RtmpSharp.IO.AMF0;
using RtmpSharp.IO.AMF0.AMFWriters;
using RtmpSharp.IO.AMF3;
using RtmpSharp.IO.AMF3.AMFWriters;
using RtmpSharp.IO.Extensions;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Xml.Linq;

namespace RtmpSharp.IO.Extensions
{
    static class Extensions
    {
        public static IList ToList(this IEnumerable enumerable)
        {
            var list = new List<object>();
            var enumerator = enumerable.GetEnumerator();
            while (enumerator.MoveNext())
                list.Add(enumerator.Current);
            return list;
        }
    }
}

namespace RtmpSharp.IO
{
    class AmfWriterMap : Dictionary<Type, IAmfItemWriter>
    {
        public IAmfItemWriter DefaultWriter { get; private set; }

        public AmfWriterMap(IAmfItemWriter defaultWriter)
        {
            DefaultWriter = defaultWriter;
        }
    }

    // Unless otherwise stated, methods in this class **do not** write type markers.
    public class AmfWriter : IDisposable
    {
        // [0, 2^29-1]
        static int[] UInt29Range = new[] { 0, 536870911 };
        // [-2^28, 2^28-1]
        static int[] Int29Range = new[] { -268435456, 268435455 };
        static DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

        static readonly AmfWriterMap Amf0Writers;
        static readonly AmfWriterMap Amf3Writers;
        
        public SerializationContext SerializationContext { get; private set; }

        readonly BinaryWriter underlying;
        readonly ObjectEncoding objectEncoding;
        readonly Dictionary<object, int> amf0ObjectReferences;
        readonly Dictionary<object, int> amf3ObjectReferences;
        readonly Dictionary<object, int> amf3StringReferences;
        readonly Dictionary<ClassDescription, int> amf3ClassDefinitionReferences;

        static AmfWriter()
        {
            var smallIntTypes = new[]
            {
                typeof(SByte),
                typeof(Byte),
                typeof(Int16),
                typeof(UInt16),
                typeof(Int32),
                typeof(UInt32)
            };
            
            var bigOrFloatingTypes = new[]
            {
                typeof(Int64),
                typeof(UInt64),
                typeof(Single),
                typeof(Double),
                typeof(Decimal)
            };

            Amf0Writers = new AmfWriterMap(new Amf0ObjectWriter())
            {
                { typeof(Array),        new Amf0ArrayWriter() },
                { typeof(AsObject),     new Amf0AsObjectWriter() },
                { typeof(bool),         new Amf0BooleanWriter() },
                { typeof(char),         new Amf0CharWriter() },
                { typeof(DateTime),     new Amf0DateTimeWriter() },
                { typeof(Enum),         new Amf0EnumWriter() },
                { typeof(Guid),         new Amf0GuidWriter() },
                { typeof(string),       new Amf0StringWriter() },
                { typeof(XDocument),    new Amf0XDocumentWriter() },
                { typeof(XElement),     new Amf0XElementWriter() },
            };

            var amf0NumberWriter = new Amf0NumberWriter();
            foreach (var type in smallIntTypes.Concat(bigOrFloatingTypes))
                Amf0Writers.Add(type, amf0NumberWriter);

            Amf3Writers = new AmfWriterMap(new Amf3ObjectWriter())
            {
                { typeof(Array),        new Amf3ArrayWriter() },
                { typeof(AsObject),     new Amf3AsObjectWriter() },
                { typeof(bool),         new Amf3BooleanWriter() },
                { typeof(ByteArray),    new Amf3ByteArrayWriter() },
                { typeof(char),         new Amf3CharWriter() },
                { typeof(DateTime),     new Amf3DateTimeWriter() },
                { typeof(Enum),         new Amf3EnumWriter() },
                { typeof(Guid),         new Amf3GuidWriter() },
                { typeof(string),       new Amf3StringWriter() },
                { typeof(XDocument),    new Amf3XDocumentWriter() },
                { typeof(XElement),     new Amf3XElementWriter() },
                { typeof(byte[]),       new Amf3NativeByteArrayWriter() },

                // `IDictionary`s are handled in the object writer
            };

            var amf3IntWriter = new Amf3IntWriter();
            foreach (var type in smallIntTypes)
                Amf3Writers.Add(type, amf3IntWriter);

            var amf3FloatingWriter = new Amf3DoubleWriter();
            foreach (var type in bigOrFloatingTypes)
                Amf3Writers.Add(type, amf3FloatingWriter);
        }

        // Add write support for the new specialized vector and dictionary types introduced into the AMF3 specification by Adobe for Flash 10
        // Old servers do not understand this, so it is an optional call.
        public static void EnableFlash10Writers()
        {
            var createVectorIntWriter = new Func<bool, IAmfItemWriter>(isFixed => new Amf3VectorWriter<int>(Amf3TypeMarkers.VectorInt, (writer, list) => writer.WriteAmf3Vector<int>(false, isFixed, list, writer.WriteInt32)));
            var createVectorUIntWriter = new Func<bool, IAmfItemWriter>(isFixed => new Amf3VectorWriter<uint>(Amf3TypeMarkers.VectorInt, (writer, list) => writer.WriteAmf3Vector<uint>(false, isFixed, list, i => writer.WriteInt32((int)i))));
            var createVectorDoubleWriter = new Func<bool, IAmfItemWriter>(isFixed => new Amf3VectorWriter<double>(Amf3TypeMarkers.VectorInt, (writer, list) => writer.WriteAmf3Vector<double>(false, isFixed, list, writer.WriteDouble)));
            var createVectorObjectWriter = new Func<bool, IAmfItemWriter>(isFixed => new Amf3VectorWriter<object>(Amf3TypeMarkers.VectorInt, (writer, list) => writer.WriteAmf3Vector<object>(true, isFixed, list, writer.WriteAmf3Item)));
            var amf3Flash10Writers = new Dictionary<Type, IAmfItemWriter>
            {
                { typeof(int[]),         createVectorIntWriter(true) },
                { typeof(List<int>),     createVectorIntWriter(false) },
                { typeof(uint[]),        createVectorUIntWriter(true) },
                { typeof(List<uint>),    createVectorUIntWriter(false) },
                { typeof(double[]),      createVectorDoubleWriter(true) },
                { typeof(List<double>),  createVectorDoubleWriter(false) },
                { typeof(object[]),      createVectorObjectWriter(true) },
                { typeof(List<object>),  createVectorObjectWriter(false) },
            };
            foreach (var pair in amf3Flash10Writers)
                Amf3Writers[pair.Key] = pair.Value;
        }

        public AmfWriter(Stream stream, SerializationContext serializationContext) : this(stream, serializationContext, ObjectEncoding.Amf3)
        {
        }

        public AmfWriter(Stream stream, SerializationContext serializationContext, ObjectEncoding objectEncoding)
        {
            this.objectEncoding = objectEncoding;
            underlying = new BinaryWriter(stream);
            amf0ObjectReferences = new Dictionary<object, int>();
            amf3ObjectReferences = new Dictionary<object, int>();
            amf3StringReferences = new Dictionary<object, int>();
            amf3ClassDefinitionReferences = new Dictionary<ClassDescription, int>();

            SerializationContext = serializationContext;
        }

        public void Dispose()
        {
            if (underlying != null)
                underlying.Dispose();
        }










        #region Helper Methods
        public long Length { get { return underlying.BaseStream.Length; } }
        public long Position { get { return underlying.BaseStream.Position; } }
        public bool DataAvailable { get { return Position < Length; } }

        static IAmfItemWriter GetAmfWriter(AmfWriterMap writerMap, Type type)
        {
            IAmfItemWriter amfWriter;

            // Use the writer specified within our dictionary, if it exists.
            if (writerMap.TryGetValue(type, out amfWriter))
                return amfWriter;

            // Try the lookup again but with the base type (so we can serialize enums and arrays,
            // for example).
            if (type.BaseType != null && writerMap.TryGetValue(type.BaseType, out amfWriter))
                return amfWriter;

            // No writer exists. Create and cache the default one so we don't need to go through this
            // expensive lookup again.
            lock (writerMap)
            {
                // Check inside lock since type may have been added since our initial check
                if (writerMap.TryGetValue(type, out amfWriter))
                    return amfWriter;

                amfWriter = writerMap.DefaultWriter;
                writerMap.Add(type, amfWriter);
                return amfWriter;
            }
        }

        public void Reset()
        {
            amf0ObjectReferences.Clear();
            amf3ObjectReferences.Clear();
            amf3StringReferences.Clear();
            amf3ClassDefinitionReferences.Clear();
        }

        public long Seek(int offset, SeekOrigin origin)
        {
            return underlying.Seek(offset, origin);
        }

        public void Flush()
        {
            underlying.Flush();
        }

        public void Write(byte value)
        {
            underlying.Write(value);
        }
        public void Write(byte[] value)
        {
            underlying.Write(value);
        }
        public void Write(byte[] bytes, int index, int count)
        {
            underlying.Write(bytes, index, count);
        }

        public void WriteByte(byte value)
        {
            Write(value);
        }

        public void WriteBytes(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            Write(buffer);
        }

        internal void WriteMarker(Amf0TypeMarkers marker)
        {
            Write((byte)marker);
        }
        internal void WriteMarker(Amf3TypeMarkers marker)
        {
            Write((byte)marker);
        }

        public void WriteInt16(short value)
        {
            var bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }

        public void WriteUInt16(ushort value)
        {
            var bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }

        public void WriteDouble(double value)
        {
            var bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }

        public void WriteFloat(float value)
        {
            var bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }

        public void WriteInt32(int value)
        {
            var bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }

        public void WriteUInt32(uint value)
        {
            var bytes = BitConverter.GetBytes(value);
            WriteBigEndian(bytes);
        }

        public void WriteReverseInt(int value)
        {
            var bytes = new byte[4];
            bytes[3] = (byte)(0xFF & (value >> 24));
            bytes[2] = (byte)(0xFF & (value >> 16));
            bytes[1] = (byte)(0xFF & (value >> 8));
            bytes[0] = (byte)(0xFF & value);
            this.Write(bytes, 0, bytes.Length);
        }

        // Writes a 32-bit signed integer to the current position in the AMF stream using variable length unsigned 29-bit integer encoding.
        public void WriteUInt24(int value)
        {
            if (value < UInt29Range[0] || value > UInt29Range[1])
                throw new ArgumentOutOfRangeException("value");

            var bytes = new byte[3];
            bytes[0] = (byte)(0xFF & (value >> 16));
            bytes[1] = (byte)(0xFF & (value >> 8));
            bytes[2] = (byte)(0xFF & (value >> 0));
            WriteBytes(bytes);
        }

        public void WriteBoolean(bool value)
        {
            Write(value ? (byte)1 : (byte)0);
        }

        // string with 16-bit length prefix
        internal void WriteUtfPrefixed(string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");

            var bytes = Encoding.UTF8.GetBytes(str);
            WriteUtfPrefixed(bytes);
        }
        void WriteUtfPrefixed(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            // TODO: More specific exception type
            if (buffer.Length > ushort.MaxValue)
                throw new SerializationException("String is larger than maximum encodable value.");
            WriteUInt16((ushort)buffer.Length);
            Write(buffer);
        }


        void WriteBigEndian(byte[] bytes)
        {
            if (BitConverter.IsLittleEndian)
                Array.Reverse(bytes);
            Write(bytes);
        }

        #endregion










        #region Both

        /// <summary>
        /// Writes an object, starting in AMF0 encoding. If the AmfWriter's `objectEncoding` is 3,
        /// then an AMF3 marker will be written and encoding will be upgraded to AMF3. Otherwise,
        /// encoding will stay in AMF0. This method writes the type marker and string.
        /// </summary>
        public void WriteAmfItem(object data)
        {
            WriteAmfItem(objectEncoding, data);
        }

        // This method is required because of the functionality specified by classes like `IDataOutput`.
        public void WriteAmfItem(ObjectEncoding objectEncoding, object data)
        {
            // Short circuit - no need to perform expensive operations to write a null
            if (data == null)
            {
                WriteMarker(Amf0TypeMarkers.Null);
                return;
            }

            if (WriteAmf0ReferenceOnExistence(data))
                return;
            var type = data.GetType();

            switch (objectEncoding)
            {
                case ObjectEncoding.Amf0:
                    var writer = GetAmfWriter(Amf0Writers, type);
                    writer.WriteData(this, data);
                    break;
                case ObjectEncoding.Amf3:
                    WriteMarker(Amf0TypeMarkers.Amf3Object);
                    WriteAmf3Item(data);
                    break;
                default:
                    throw new ArgumentOutOfRangeException("objectEncoding");
            }
        }

        #endregion










        #region AMF0

        internal void AddAmf0Reference(object value)
        {
            amf0ObjectReferences.Add(value, amf0ObjectReferences.Count);
        }

        /// <summary>This method writes the type marker and string.</summary>
        internal bool WriteAmf0ReferenceOnExistence(object value)
        {
            int index;
            if (amf0ObjectReferences.TryGetValue(value, out index))
            {
                WriteMarker(Amf0TypeMarkers.Reference);
                WriteUInt16((ushort)amf0ObjectReferences[value]);
                return true;
            }
            return false;
        }

        /// <summary>This method writes the type marker and string.</summary>
        internal void WriteAmf0StringSpecial(string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");

            var bytes = Encoding.UTF8.GetBytes(str);
            var length = bytes.Length;
            if (length < ushort.MaxValue)
            {
                WriteMarker(Amf0TypeMarkers.String);
                WriteUtfPrefixed(bytes);
            }
            else
            {
                WriteMarker(Amf0TypeMarkers.LongString);
                WriteAmf0UtfLong(bytes);
            }
        }

        internal void WriteAmf0UtfLong(string value)
        {
            WriteAmf0UtfLong(Encoding.UTF8.GetBytes(value));
        }
        void WriteAmf0UtfLong(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            // length written as 32-bit uint
            WriteUInt32((uint)buffer.Length);
            WriteBytes(buffer);
        }

        /// <summary>This method writes the type marker and string.</summary>
        public void WriteAmf0Item(object data)
        {
            // Short circuit - no need to perform expensive operations to write a null
            if (data == null)
            {
                WriteMarker(Amf0TypeMarkers.Null);
                return;
            }


            if (WriteAmf0ReferenceOnExistence(data))
                return;
            var type = data.GetType();

            GetAmfWriter(Amf0Writers, type).WriteData(this, data);
        }

        /// <summary>This method writes the type marker and string.</summary>
        internal void WriteAmf0AsObject(AsObject obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            AddAmf0Reference(obj);
            var anonymousObject = string.IsNullOrEmpty(obj.TypeName);
            WriteMarker(anonymousObject ? Amf0TypeMarkers.Object : Amf0TypeMarkers.TypedObject);
            if (!anonymousObject)
                WriteUtfPrefixed(obj.TypeName);

            foreach (var property in obj)
            {
                WriteUtfPrefixed(property.Key);
                WriteAmf0Item(property.Value);
            }

            // End of object denoted by zero-length field name, then end of object type marker
            // Field names are length-prefixed UTF8 strings, so [0 length string, end of object type marker]
            WriteUInt16(0);
            WriteMarker(Amf0TypeMarkers.ObjectEnd);
        }

        /// <summary>This method writes the type marker and string.</summary>
        internal void WriteAmf0TypedObject(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            if (SerializationContext == null)
                throw new NullReferenceException("Cannot serialize objects because no SerializationContext was provided.");

            AddAmf0Reference(obj);

            var type = obj.GetType();
            var typeName = type.FullName;

            var classDescription = SerializationContext.GetClassDescription(type, obj);
            if (classDescription == null)
                throw new SerializationException(string.Format("Couldn't get class description for {0}.", typeName));

            WriteMarker(Amf0TypeMarkers.TypedObject);
            WriteUtfPrefixed(classDescription.Name);
            foreach (var member in classDescription.Members)
            {
                WriteUtfPrefixed(member.SerializedName);
                WriteAmf0Item(member.GetValue(obj));
            }

            // End of object denoted by zero-length field name, then end of object type marker
            // Field names are length-prefixed UTF8 strings, so [0 length string, end of object type marker]
            WriteUInt16(0);
            WriteMarker(Amf0TypeMarkers.ObjectEnd);
        }

        internal void WriteAmf0DateTime(DateTime value)
        {
            // http://download.macromedia.com/pub/labs/amf/amf0_spec_121207.pdf
            // """
            // While the design of this type reserves room for time zone offset information,
            // it should not be filled in, nor used, as it is unconventional to change time
            // zones when serializing dates on a network. It is suggested that the time zone
            // be queried independently as needed.
            //  -- AMF0 specification, 2.13 Date Type
            // """

            var time = value.ToUniversalTime();
            var posixTime = time.Subtract(epoch);
            WriteDouble((double)posixTime.TotalMilliseconds);
            // reserved for time zone info, but not used according to spec.
            WriteUInt16(0);
        }

        internal void WriteAmf0XDocument(XDocument document)
        {
            if (document == null)
                throw new ArgumentNullException("document");

            AddAmf0Reference(document);
            var xml = document.ToString();
            WriteAmf0UtfLong(xml);
        }

        internal void WriteAmf0XElement(XElement element)
        {
            if (element == null)
                throw new ArgumentNullException("element");

            AddAmf0Reference(element);
            var xml = element.ToString();
            WriteAmf0UtfLong(xml);
        }

        internal void WriteAmf0Array(Array array)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            AddAmf0Reference(array);
            WriteInt32(array.Length);
            foreach (var element in array)
                WriteAmf0Item(element);
        }

        /// <summary>This method writes the type marker and string.</summary>
        internal void WriteAmf0AssociativeArray(IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            AddAmf0Reference(dictionary);
            WriteMarker(Amf0TypeMarkers.EcmaArray);
            WriteInt32(dictionary.Count);
            foreach (var entry in dictionary)
            {
                WriteUtfPrefixed(entry.Key);
                WriteAmf0Item(entry.Value);
            }

            // End of object denoted by zero-length field name, then end of object type marker
            // Field names are length-prefixed UTF8 strings, so [0 length string, end of object type marker]
            WriteUInt16(0);
            WriteMarker(Amf0TypeMarkers.ObjectEnd);
        }

        #endregion










        #region AMF3

        void AddAmf3Reference(string obj)
        {
            AddAmf3Reference(amf3StringReferences, obj);
        }
        void AddAmf3Reference(object obj)
        {
            AddAmf3Reference(amf3ObjectReferences, obj);
        }
        void AddAmf3Reference(Dictionary<object, int> referenceDictionary, object obj)
        {
            referenceDictionary.Add(obj, referenceDictionary.Count);
        }
        // Writes `value` with the `inline object flag`. The object contents is expected to be written after this header.
        void WriteAmf3InlineHeader(int value)
        {
            // 1 == inline object (not an object reference)
            WriteAmf3Int((value << 1) | 1);
        }

        // If `obj` has already been written, then write the reference and returns true. If no object was written, returns false.
        bool WriteAmf3ReferenceOnExistence(string obj)
        {
            return WriteAmf3ReferenceOnExistence(amf3StringReferences, obj);
        }
        bool WriteAmf3ReferenceOnExistence(object obj)
        {
            return WriteAmf3ReferenceOnExistence(amf3ObjectReferences, obj);
        }
        bool WriteAmf3ReferenceOnExistence(Dictionary<object, int> referenceDictionary, object obj)
        {
            int index;
            if (referenceDictionary.TryGetValue(obj, out index))
            {
                // 0 == not inline (an object reference)
                WriteAmf3Int(index << 1);
                return true;
            }
            return false;
        }

        /// <summary>This method writes the type marker and string.</summary>
        public void WriteAmf3Item(object data)
        {
            // Short circuit - no need to perform expensive operations to write a null
            if (data == null)
            {
                WriteAmf3Null();
                return;
            }

            var type = data.GetType();
            var writer = GetAmfWriter(Amf3Writers, type);
            writer.WriteData(this, data);
        }

        /// <summary>This method writes the type marker and string.</summary>
        internal void WriteAmf3Null()
        {
            WriteMarker(Amf3TypeMarkers.Null);
        }

        /// <summary>This method writes the type marker and string.</summary>
        internal void WriteAmf3BoolSpecial(bool value)
        {
            WriteMarker(value ? Amf3TypeMarkers.True : Amf3TypeMarkers.False);
        }

        internal void WriteAmf3Array(Array array)
        {
            if (array == null)
                throw new ArgumentNullException("array");

            if (WriteAmf3ReferenceOnExistence(array))
                return;

            AddAmf3Reference(array);
            WriteAmf3InlineHeader(array.Length);

            // empty key signifies end of associative section of array
            WriteAmf3Utf(string.Empty);
            foreach (var element in array)
                WriteAmf3Item(element);
        }

        internal void WriteAmf3Array(IEnumerable enumerable)
        {
            if (enumerable == null)
                throw new ArgumentNullException("enumerable");

            if (WriteAmf3ReferenceOnExistence(enumerable))
                return;

            var list = enumerable.ToList();
            AddAmf3Reference(list);
            // Number of dense items.
            WriteAmf3InlineHeader(list.Count);

            // empty key signifies end of associative section of array
            WriteAmf3Utf(string.Empty);
            foreach (var element in list)
                WriteAmf3Item(element);
        }

        internal void WriteAmf3AssociativeArray(IDictionary<string, object> dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            if (WriteAmf3ReferenceOnExistence(dictionary))
                return;

            AddAmf3Reference(dictionary);
            // Number of dense items - zero for an associative array.
            WriteAmf3InlineHeader(0);

            foreach (var pair in dictionary)
            {
                WriteAmf3Utf(pair.Key);
                WriteAmf3Item(pair.Value);
            }

            // empty key signifies end of associative section of array
            WriteAmf3Utf(string.Empty);
        }

        internal void WriteAmf3ByteArray(ByteArray byteArray)
        {
            if (byteArray == null)
                throw new ArgumentNullException("byteArray");

            if (WriteAmf3ReferenceOnExistence(byteArray))
                return;

            AddAmf3Reference(byteArray);
            WriteAmf3InlineHeader((int)byteArray.Length);
            WriteBytes(byteArray.MemoryStream.ToArray());
        }

        internal void WriteAmf3Utf(string str)
        {
            if (str == null)
                throw new ArgumentNullException("str");

            if (str == string.Empty)
            {
                // zero length strings are never sent by reference.
                WriteAmf3InlineHeader(0);
                return;
            }

            if (WriteAmf3ReferenceOnExistence(str))
                return;

            AddAmf3Reference(str);
            var bytes = Encoding.UTF8.GetBytes(str);
            WriteAmf3InlineHeader(bytes.Length);
            WriteBytes(bytes);
        }

        internal void WriteAmf3DateTime(DateTime value)
        {
            if (WriteAmf3ReferenceOnExistence(value))
                return;

            var time = value.ToUniversalTime();
            var posixTime = time.Subtract(epoch);
            // not used except to denote inline object
            WriteAmf3InlineHeader(0);
            WriteDouble((double)posixTime.TotalMilliseconds);
        }

        // when writing, sign does not matter.
        internal void WriteAmf3Int(int value)
        {
            // sign contraction - the high order bit of the resulting value must match every bit removed from the number
            // clear 3 bits
            value = value & 0x1fffffff;
            if (value < 0x80)
            {
                WriteByte((byte)value);
            }
            else if (value < 0x4000)
            {
                WriteByte((byte)(value >> 7 & 0x7f | 0x80));
                WriteByte((byte)(value & 0x7f));
            }
            else if (value < 0x200000)
            {
                WriteByte((byte)(value >> 14 & 0x7f | 0x80));
                WriteByte((byte)(value >> 7 & 0x7f | 0x80));
                WriteByte((byte)(value & 0x7f));
            }
            else
            {
                WriteByte((byte)(value >> 22 & 0x7f | 0x80));
                WriteByte((byte)(value >> 15 & 0x7f | 0x80));
                WriteByte((byte)(value >> 8 & 0x7f | 0x80));
                WriteByte((byte)(value & 0xff));
            }
        }

        /// <summary>This method writes the type marker and string.</summary>
        internal void WriteAmf3NumberSpecial(int value)
        {
            // Write numbers that are out of range as a double.
            if (value >= Int29Range[0] && value <= Int29Range[1])
            {
                WriteMarker(Amf3TypeMarkers.Integer);
                WriteAmf3Int(value);
            }
            else
            {
                WriteMarker(Amf3TypeMarkers.Double);
                WriteAmf3Double((double)value);
            }
        }

        internal void WriteAmf3Double(double value)
        {
            WriteDouble(value);
        }

        internal void WriteAmf3XDocument(XDocument document)
        {
            //if (document == null)
            //    throw new ArgumentNullException("document");

            var flattened = document == null ? string.Empty : document.ToString();
            WriteAmf3Utf(flattened);
        }

        internal void WriteAmf3XElement(XElement element)
        {
            //if (element == null)
            //    throw new ArgumentNullException("element");

            var flattened = element == null ? string.Empty : element.ToString();
            WriteAmf3Utf(flattened);
        }

        internal void WriteAmf3Vector<T>(bool writeTypeName, bool fixedSize, IList list, Action<T> writeElement)
        {
            if (list == null)
                throw new ArgumentNullException("list");

            if (WriteAmf3ReferenceOnExistence(list))
                return;

            AddAmf3Reference(list);
            WriteAmf3InlineHeader(list.Count);

            WriteByte((byte)(fixedSize ? 1 : 0));
            // the "any type"
            if (writeTypeName)
                WriteAmf3Utf("*");
            foreach (var item in list)
                writeElement((T)item);
        }

        internal void WriteAmf3Dictionary(IDictionary dictionary)
        {
            if (dictionary == null)
                throw new ArgumentNullException("dictionary");

            if (WriteAmf3ReferenceOnExistence(dictionary))
                return;

            var itemCount = dictionary.Count;

            AddAmf3Reference(dictionary);
            WriteAmf3InlineHeader(itemCount);

            // we don't support weakly referenced pairs (yet) - always use strong references
            WriteByte((byte)0);
            var enumerator = dictionary.GetEnumerator();
            while (enumerator.MoveNext())
            {
                WriteAmf3Item(enumerator.Key);
                WriteAmf3Item(enumerator.Value);
            }
        }


        internal void WriteAmf3Object(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException("obj");

            if (SerializationContext == null)
                throw new NullReferenceException("Cannot serialize objects because no SerializationContext was provided.");

            if (WriteAmf3ReferenceOnExistence(obj))
                return;
            AddAmf3Reference(obj);

            var classDescription = SerializationContext.GetClassDescription(obj);
            int existingDefinitionIndex;
            if (amf3ClassDefinitionReferences.TryGetValue(classDescription, out existingDefinitionIndex))
            {
                // http://download.macromedia.com/pub/labs/amf/amf3_spec_121207.pdf
                // """
                // The first (low) bit is a flag with value 1. The second bit is a flag
                // (representing whether a trait reference follows) with value 0 to imply that
                // this objects traits are  being sent by reference. The remaining 1 to 27
                // significant bits are used to encode a trait reference index (an  integer).
                // -- AMF3 specification, 3.12 Object type
                // """

                // <u27=trait-reference-index> <0=trait-reference> <1=object-inline>
                WriteAmf3InlineHeader(existingDefinitionIndex << 1);
            }
            else
            {
                amf3ClassDefinitionReferences.Add(classDescription, amf3ClassDefinitionReferences.Count);

                // write the class definition
                // we can use the same format to serialize normal and extern classes, for simplicity's sake.
                //     normal:         <u25=member-count> <u1=dynamic> <0=externalizable> <1=trait-inline> <1=object-inline>
                //     externalizable: <u25=insignificant> <u1=insignificant> <1=externalizable> <1=trait-inline> <1=object-inline>
                var header = classDescription.Members.Length;
                header = (header << 1) | (classDescription.IsDynamic ? 1 : 0);
                header = (header << 1) | (classDescription.IsExternalizable ? 1 : 0);
                header = (header << 1) | 1;
                // last shift done in this method
                WriteAmf3InlineHeader(header);
                WriteAmf3Utf(classDescription.Name);

                // write object
                if (classDescription.IsExternalizable)
                {
                    var externalizable = obj as IExternalizable;
                    if (externalizable == null)
                        throw new SerializationException("Externalizable class does not implement IExternalizable");

                    externalizable.WriteExternal(new DataOutput(this));
                }
                else
                {
                    foreach (var member in classDescription.Members)
                        WriteAmf3Utf(member.SerializedName);

                    foreach (var member in classDescription.Members)
                        WriteAmf3Item(member.GetValue(obj));

                    if (classDescription.IsDynamic)
                    {
                        var dictionary = obj as IDictionary<string, object>;
                        if (dictionary == null)
                            throw new SerializationException("Dynamic class does not implement IDictionary");

                        foreach (KeyValuePair<string, object> entry in dictionary)
                        {
                            WriteAmf3Utf(entry.Key);
                            WriteAmf3Item(entry.Value);
                        }
                        WriteAmf3Utf(string.Empty);
                    }
                }
            }
        }

        #endregion AMF3
    }
}
