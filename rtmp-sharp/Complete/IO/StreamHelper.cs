using System;
using System.IO;
using System.Threading.Tasks;

// astralfoxy:complete/io/streamhelper.cs
namespace Complete.IO
{
    static class StreamHelper
    {
        public static byte[] ReadBytes(this Stream stream, int count)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var result = new byte[count];
            var bytesRead = 0;
            while (count > 0)
            {
                var n = stream.Read(result, bytesRead, count);
                if (n == 0)
                    break;
                bytesRead += n;
                count -= n;
            }

            if (bytesRead != result.Length)
                throw new EndOfStreamException();

            return result;
        }

        public static async Task<byte[]> ReadBytesAsync(this Stream stream, int count)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var result = new byte[count];
            var bytesRead = 0;
            while (count > 0)
            {
                var n = await stream.ReadAsync(result, bytesRead, count).ConfigureAwait(false);
                if (n == 0)
                    break;
                bytesRead += n;
                count -= n;
            }

            if (bytesRead != result.Length)
                throw new EndOfStreamException();

            return result;
        }

        public static async Task<byte> ReadByteAsync(this Stream stream)
        {
            var buffer = new byte[1];
            var read = await stream.ReadAsync(buffer, 0, 1);
            if (read == 0)
                throw new EndOfStreamException();
            return buffer[0];
        }
    }
}
