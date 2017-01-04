using System.IO;
using System.Threading;
using System.Threading.Tasks;

// csharp: hina/io/extensions/streamextensions.cs [snipped]
namespace Hina.IO
{
    public static class StreamExtensions
    {
        public static byte[] ReadBytes(this Stream stream, int count)
        {
            CheckDebug.NotNull(stream);

            var buffer = new byte[count];
            stream.ReadBytes(buffer, 0, count);

            return buffer;
        }

        public static void ReadBytes(this Stream stream, byte[] buffer, int index, int count)
        {
            CheckDebug.NotNull(stream, buffer);

            var read = 0;

            while (count > 0)
            {
                var n = stream.Read(buffer, read, count);

                if (n == 0)
                    break;

                read += n;
                count -= n;
            }

            if (count != 0) throw new EndOfStreamException();
        }

        public static async Task<byte[]> ReadBytesAsync(this Stream stream, int count)
        {
            CheckDebug.NotNull(stream);

            var buffer = new byte[count];
            await stream.ReadBytesAsync(buffer, 0, count);

            return buffer;
        }

        public static async Task ReadBytesAsync(this Stream stream, byte[] buffer, int index, int count)
        {
            CheckDebug.NotNull(stream, buffer);

            var read = 0;

            while (count > 0)
            {
                var n = await stream.ReadAsync(buffer, read, count);

                if (n == 0)
                    break;

                read += n;
                count -= n;
            }

            if (count != 0)
                throw new EndOfStreamException();
        }


        public static void Write(this Stream stream, byte[] buffer)
            => stream.Write(CheckDebug.NotNull(buffer), 0, buffer.Length);

        public static Task WriteAsync(this Stream stream, byte[] buffer)
            => stream.WriteAsync(CheckDebug.NotNull(buffer), 0, buffer.Length);

        public static Task WriteAsync(this Stream stream, byte[] buffer, CancellationToken cancellationToken)
            => stream.WriteAsync(CheckDebug.NotNull(buffer), 0, buffer.Length, cancellationToken);
    }
}