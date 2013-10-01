using System;
using System.IO;
using System.Threading.Tasks;

namespace RtmpSharp.IO
{
    static class StreamHelper
    {
        public static byte[] ReadBytes(Stream stream, int count)
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
            {
                throw new EndOfStreamException();
                // Trim array.  This should happen on EOF & possibly net streams. 
                //var copy = new byte[numRead];
                //Buffer.InternalBlockCopy(result, 0, copy, 0, numRead);
                //result = copy;
            }

            return result;
        }

        public static async Task<byte[]> ReadBytesAsync(Stream stream, int count)
        {
            if (stream == null)
                throw new ArgumentNullException("stream");

            var result = new byte[count];
            var bytesRead = 0;
            while (count > 0)
            {
                var n = await stream.ReadAsync(result, bytesRead, count);
                if (n == 0)
                    break;
                bytesRead += n;
                count -= n;
            }

            if (bytesRead != result.Length)
            {
                throw new EndOfStreamException();
                // Trim array.  This should happen on EOF & possibly net streams. 
                //var copy = new byte[numRead];
                //Buffer.InternalBlockCopy(result, 0, copy, 0, numRead);
                //result = copy;
            }

            return result;
        }
    }
}
