using System;
using System.Collections.Generic;
using Hina;
using Hina.IO;

namespace RtmpSharp.IO
{
    public partial class AmfWriter
    {
        readonly ByteWriter writer;
        readonly SerializationContext context;

        readonly Base core;
        readonly Amf0 amf0;
        readonly Amf3 amf3;

        public int Length   => writer.Length;
        public int Position => writer.Position;


        public AmfWriter(SerializationContext context)
            : this(new ByteWriter(), context, 0) { }

        public AmfWriter(byte[] data, SerializationContext context)
            : this(new ByteWriter(data), context, 0) { }

        public AmfWriter(int initialBufferLengthHint, SerializationContext context)
            : this(new ByteWriter(initialBufferLengthHint), context, 0) { }

        AmfWriter(ByteWriter writer, SerializationContext context, byte _)
        {
            this.context = Check.NotNull(context);
            this.writer  = writer;

            this.core = new Base(writer);
            this.amf3 = new Amf3(context, this, core);
            this.amf0 = new Amf0(context, core, amf3);

        }


        public Space<byte> Span => writer.Span;
        public void   Return()  => writer.Return();
        public byte[] ToArray() => writer.ToArray();
        public byte[] ToArrayAndReturn() => writer.ToArrayAndReturn();



        // efficiency: avoid re-allocating this object by re-binding it to a new buffer, effectively resetting this object.

        public void Reset()
        {
            amf0.Reset();
            amf3.Reset();
            writer.Reset();
        }

        public void Rebind(Space<byte> data)
        {
            amf0.Reset();
            amf3.Reset();
            writer.Buffer = data;
        }

        public void Rebind(byte[] data)
        {
            Rebind(new Space<byte>(data));
        }



        public void WriteByte(byte value)                           => core.WriteByte(value);
        public void WriteBytes(Space<byte> span)                    => core.WriteBytes(span);
        public void WriteBytes(byte[] buffer)                       => core.WriteBytes(buffer);
        public void WriteBytes(byte[] buffer, int index, int count) => core.WriteBytes(buffer, index, count);
        public void WriteInt16(short value)                         => core.WriteInt16(value);
        public void WriteUInt16(ushort value)                       => core.WriteUInt16(value);
        public void WriteInt32(int value)                           => core.WriteInt32(value);
        public void WriteUInt32(uint value)                         => core.WriteUInt32(value);
        public void WriteLittleEndianInt(uint value)                => core.WriteLittleEndianInt(value);
        public void WriteUInt24(uint value)                         => core.WriteUInt24(value);
        public void WriteBoolean(bool value)                        => core.WriteBoolean(value);
        public void WriteDouble(double value)                       => core.WriteDouble(value);
        public void WriteSingle(float value)                        => core.WriteSingle(value);
        public void WriteUtfPrefixed(string value)                  => core.WriteUtfPrefixed(value);
        public void WriteUtfPrefixed(byte[] utf8)                   => core.WriteUtfPrefixed(utf8);
        public void WriteAmf0Object(object value)                   => amf0.WriteItem(value);
        public void WriteAmf3Object(object value)                   => amf3.WriteItem(value);

        public void WriteBoxedAmf0Object(ObjectEncoding encoding, object value)
            => amf0.WriteBoxedItem(encoding, value);

        public void WriteAmfObject(ObjectEncoding encoding, object value)
        {
            if (encoding == ObjectEncoding.Amf0)
                amf0.WriteItem(value);
            else if (encoding == ObjectEncoding.Amf3)
                amf3.WriteItem(value);
            else
                throw new ArgumentOutOfRangeException("unsupported encoding");
        }


        class ReferenceList<T> : Dictionary<T, ushort>
        {
            // returns true if there was already a reference to this object, false otherwise. `index` is always set to
            // the index for object `obj` regardless of return value.
            public bool Add(T obj, out ushort index)
            {
                const int MaximumReferences = 65535;

                if (TryGetValue(obj, out index))
                    return true;

                var count = Count;
                if (count >= MaximumReferences)
                {
                    index = 0;
                    return false;
                }

                Add(obj, index = (ushort)count);
                return false;
            }

            public int GetIndex(T obj) => this[obj];
        }
    }
}
