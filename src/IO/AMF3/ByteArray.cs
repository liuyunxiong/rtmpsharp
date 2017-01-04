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
            using (var output = new MemoryStream())
            using (var stream = algorithm == Compression.Zlib ? new ZlibStream(output, CompressionMode.Compress, true) : new DeflateStream(output, CompressionMode.Compress, true))
            {
                stream.Write(Buffer.Array, Buffer.Offset, Buffer.Count);
                Buffer = output.GetBuffer();
            }
        }

        public void Uncompress(Compression algorithm)
        {
            using (var input  = new MemoryStream(Buffer.Array, Buffer.Offset, Buffer.Count, false))
            using (var stream = algorithm == Compression.Zlib ? new ZlibStream(input, CompressionMode.Decompress, false) : new DeflateStream(input, CompressionMode.Decompress, false))
            using (var output = new MemoryStream())
            {
                stream.CopyTo(output);
                Buffer = output.GetBuffer();
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
