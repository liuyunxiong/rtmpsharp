using System;
using System.ComponentModel;
using System.IO;
using System.IO.Compression;
using Hina;
using Hina.IO;
using Hina.IO.Zlib;

namespace RtmpSharp.IO.AMF3
{
    [TypeConverter(typeof(ByteArrayConverter))]
    [RtmpSharp("flex.messaging.io.ByteArray")]
    public class ByteArray
    {
        public ArraySegment<byte> Buffer;

        public ByteArray()                          { }
        public ByteArray(byte[] buffer)             => Buffer = new ArraySegment<byte>(buffer);
        public ByteArray(ArraySegment<byte> buffer) => Buffer = buffer;

        // returns a copy of the underlying buffer
        public byte[] ToArray()  => Buffer.ToArray();

        public void Deflate()    => Compress(Compression.Deflate);
        public void Inflate()    => Uncompress(Compression.Deflate);

        public void Compress()   => Compress(Compression.Zlib);
        public void Uncompress() => Uncompress(Compression.Zlib);

        public void Compress(Compression algorithm)
        {
            using (var memory = new MemoryStream())
            using (var stream = algorithm == Compression.Zlib ? new ZlibStream(memory, CompressionMode.Compress, true) : new DeflateStream(memory, CompressionMode.Compress, true))
            {
                stream.Write(Buffer.Array, Buffer.Offset, Buffer.Count);
                Buffer = new ArraySegment<byte>(memory.GetBuffer(), offset: 0, count: (int)memory.Length);
            }
        }

        public void Uncompress(Compression algorithm)
        {
            using (var source       = new MemoryStream(Buffer.Array, Buffer.Offset, Buffer.Count, false))
            using (var intermediate = algorithm == Compression.Zlib ? new ZlibStream(source, CompressionMode.Decompress, false) : new DeflateStream(source, CompressionMode.Decompress, false))
            using (var memory       = new MemoryStream())
            {
                intermediate.CopyTo(memory);
                Buffer = new ArraySegment<byte>(memory.GetBuffer(), offset: 0, count: (int)memory.Length);
            }
        }

        public enum Compression { Deflate, Zlib }
    }

    public class ByteArrayConverter : TypeConverter
    {
        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(byte[]))
                return true;

            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object source, Type destinationType)
        {
            if (destinationType == typeof(byte[]))
            {
                Check.NotNull(source);
                return (source as ByteArray).ToArray();
            }

            return base.ConvertTo(context, culture, source, destinationType);
        }
    }
}
