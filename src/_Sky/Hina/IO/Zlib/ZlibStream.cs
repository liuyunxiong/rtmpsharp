using System;
using System.IO;
using System.IO.Compression;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

// csharp: hina/io/zlib/zlibstream.cs [snipped]
namespace Hina.IO.Zlib
{
    // implements a subset of the `zlib` format:
    //     - only `deflate` is supported
    //     - preset dictionaries are not supported
    public class ZlibStream : DeflateStream
    {
        static readonly byte[] ZlibHeader = { 0x58, 0x85 };

        readonly Adler32         adler32;
        readonly CompressionMode compressionMode;
        readonly bool            leaveOpen;

        bool hasRead;
        bool hasWritten;
        Stream stream;

        public ZlibStream(Stream stream, CompressionMode compressionMode) : this(stream, compressionMode, false)
        {
        }

        public ZlibStream(Stream stream, CompressionMode compressionMode, bool leaveOpen) : base(stream, compressionMode, leaveOpen)
        {
            Check.NotNull(stream);

            this.adler32         = new Adler32();
            this.stream          = stream;
            this.leaveOpen       = leaveOpen;
            this.compressionMode = compressionMode;
        }


        public override int Read(byte[] buffer, int offset, int count)
        {
            CheckDebug.NotNull(buffer);

            // the zlib format is specified by rfc 1950. Zlib also uses deflate, plus 2 or 6 header bytes, and a 4 byte checksum at the end.
            // the first 2 bytes indicate the compression method and flags. if the dictionary flag is set, then 4 additional bytes will follow.
            // preset dictionaries aren't very common and we don't support them.
            if (!hasRead)
            {
                hasRead = true;
                VerifyZlibHeader(stream.ReadBytes(2));
            }

            return base.Read(buffer, offset, count);
        }

        public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDebug.NotNull(buffer);

            if (!hasRead)
            {
                hasRead = true;
                VerifyZlibHeader(await stream.ReadBytesAsync(2));
            }

            return await base.ReadAsync(buffer, offset, count, cancellationToken);
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            CheckDebug.NotNull(buffer);

            if (!hasWritten)
            {
                hasWritten = true;
                stream.Write(ZlibHeader, 0, ZlibHeader.Length);
            }

            base.Write(buffer, offset, count);
            adler32.Update(buffer, offset, count);
        }

        public override async Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
        {
            CheckDebug.NotNull(buffer);

            if (!hasWritten)
            {
                hasWritten = true;
                await stream.WriteAsync(ZlibHeader, 0, ZlibHeader.Length, cancellationToken);
            }

            await base.WriteAsync(buffer, offset, count, cancellationToken);
            adler32.Update(buffer, offset, count);
        }


        protected override void Dispose(bool disposing)
        {
            if (disposing && stream != null && compressionMode == CompressionMode.Compress)
            {
                var bytes = GetChecksumBytes();
                stream.Write(bytes, 0, bytes.Length);

                stream = null;
            }

            base.Dispose(disposing);
        }


        byte[] GetChecksumBytes()
        {
            var checksum = IPAddress.HostToNetworkOrder(adler32.Checksum);
            return BitConverter.GetBytes(checksum);
        }

        static void VerifyZlibHeader(byte[] header)
        {
            //if (header.Length != 2 || header[0] != ZlibHeader[0] || header[1] != ZlibHeader[1])
            //    throw new InvalidDataException("invalid zlib header, or supported zlib header with additional options");
        }
    }
}
