using System;
using System.Collections.Generic;
using Hina;
using Hina.IO;

namespace RtmpSharp.IO
{
    public partial class AmfReader
    {
        readonly ByteReader reader;
        readonly SerializationContext context;

        readonly Base core;
        readonly Amf0 amf0;
        readonly Amf3 amf3;

        public int Length    => reader.Length;
        public int Position  => reader.Position;
        public int Remaining => reader.Length - reader.Position;


        public AmfReader(SerializationContext context)
            : this(EmptyCollection<byte>.Array, context) { }

        public AmfReader(byte[] data, SerializationContext context)
        {
            Check.NotNull(data, context);

            this.context = context;
            this.reader  = new ByteReader(data);

            core = new Base(reader);
            amf3 = new Amf3(context, this, core);
            amf0 = new Amf0(context, core, amf3);
        }


        public bool HasLength(int count)
        {
            return core.HasLength(count);
        }
        
        // efficiency: avoid re-allocating this object by re-binding it to a new buffer, effectively resetting this object.
        public void Rebind(Space<byte> span)
        {
            reader.Span = span;

            amf0.Reset();
            amf3.Reset();
        }

        public void Rebind(byte[] data)
        {
            Rebind(new Space<byte>(data));
        }

        public void Rebind(byte[] data, int index, int count)
        {
            Rebind(new Space<byte>(data, index, count));
        }


        public Space<byte> ReadSpan(int count)                       => core.ReadSpan(count);

        public byte   ReadByte()                                     => core.ReadByte();
        public byte[] ReadBytes(int count)                           => core.ReadBytes(count);
        public void   ReadBytes(byte[] buffer, int index, int count) => core.ReadBytes(buffer, index, count);
        public ushort ReadUInt16()                                   => core.ReadUInt16();
        public short  ReadInt16()                                    => core.ReadInt16();
        public bool   ReadBoolean()                                  => core.ReadBoolean();
        public int    ReadInt32()                                    => core.ReadInt32();
        public uint   ReadUInt32()                                   => core.ReadUInt32();
        public int    ReadLittleEndianInt()                          => core.ReadLittleEndianInt();
        public uint   ReadUInt24()                                   => core.ReadUInt24();
        public double ReadDouble()                                   => core.ReadDouble();
        public float  ReadSingle()                                   => core.ReadSingle();
        public string ReadUtf()                                      => core.ReadUtf();
        public string ReadUtf(int length)                            => core.ReadUtf(length);
        public object ReadAmf0Object()                               => amf0.ReadItem();
        public object ReadAmf3Object()                               => amf3.ReadItem();

        public object ReadAmfObject(ObjectEncoding encoding)
        {
            if (encoding == ObjectEncoding.Amf0)
                return amf0.ReadItem();

            if (encoding == ObjectEncoding.Amf3)
                return amf3.ReadItem();

            throw new ArgumentOutOfRangeException("unsupported encoding");
        }


        class ReferenceList<T> : List<T>
        {
            public T Get(int index) => this[index];
        }

        class InvalidReference
        {
            public static readonly InvalidReference Instance = new InvalidReference();
        }
    }
}
