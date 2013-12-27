using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

// astralfoxy:complete/io/zlib/zlibstream.cs
namespace Complete.IO.Zlib
{
    // Implements a subset of the `zlib` format:
    //     - only `deflate` is supported
    //     - preset dictionaries are not supported; we pretend they don't exist
    public class ZlibStream : DeflateStream
    {
        static readonly byte[] ZlibHeader = new byte[] { 0x58, 0x85 };

        bool firstReadWrite;

        readonly Adler32 adler32;
        readonly CompressionMode mode;
        readonly bool leaveOpen;
        Stream stream;

        public ZlibStream(Stream stream, CompressionMode mode) : this(stream, mode, false) { }

        public ZlibStream(Stream stream, CompressionMode mode, bool leaveOpen)
            : base(stream, mode, true)
        {
            this.stream = stream;
            this.leaveOpen = leaveOpen;
            this.mode = mode;

            this.firstReadWrite = true;
            this.adler32 = new Adler32();
        }





        public override int Read(byte[] buffer, int offset, int count)
        {
            // The zlib format is specified by RFC 1950. Zlib also uses deflate, plus 2 or 6 header bytes, and a 4 byte checksum at the end. 
            // The first 2 bytes indicate the compression method and flags. If the dictionary flag is set, then 4 additional bytes will follow.
            // OHGOD: Preset dictionaries aren't very common; pretend they don't exist.
            if (firstReadWrite)
            {
                firstReadWrite = false;

                // Chop off the first two bytes
                var b1 = stream.ReadByte();
                var b2 = stream.ReadByte();
            }

            return base.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (firstReadWrite)
            {
                firstReadWrite = false;

                // Chop off the first two bytes
                var b1b2 = await stream.ReadBytesAsync(2);
            }

            return await base.ReadAsync(buffer, offset, count, cancellationToken);
        }





        public override void Write(byte[] buffer, int offset, int count)
        {
            if (firstReadWrite)
            {
                firstReadWrite = false;
                stream.Write(ZlibHeader, 0, ZlibHeader.Length);
            }

            base.Write(buffer, offset, count);
            adler32.Update(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            if (firstReadWrite)
            {
                firstReadWrite = false;
                await stream.WriteAsync(ZlibHeader, 0, ZlibHeader.Length);
            }

            await base.WriteAsync(buffer, offset, count, cancellationToken);
            adler32.Update(buffer, offset, count);
        }





        protected override void Dispose(bool disposing)
        {
            if (disposing && stream != null && mode == CompressionMode.Compress)
            {
                var bytes = GetChecksumBytes();
                stream.Write(bytes, 0, bytes.Length);

                if (!leaveOpen)
                    stream.Close();

                stream = null;
            }

            base.Dispose(disposing);
        }

        byte[] GetChecksumBytes()
        {
            var checksum = IPAddress.HostToNetworkOrder(adler32.Checksum);
            return BitConverter.GetBytes(checksum);
        }
    }
}
