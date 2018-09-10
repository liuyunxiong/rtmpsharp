using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

// csharp: hina/collections/extensions/spaceextensions.cs [snipped]
namespace Hina
{
    public static class SpaceExtensions
    {
        // encoder

        public static string GetString(this Encoding encoding, Space<byte> span)
            => encoding.GetString(span.Array, span.Offset, span.Length);


        // stream io

        public static Task<int> ReadAsync(this Stream stream, Space<byte> span)
            => ReadAsync(stream, span, CancellationToken.None);

        public static Task<int> ReadAsync(this Stream stream, Space<byte> span, CancellationToken cancellationToken)
            => stream.ReadAsync(span.Array, span.Offset, span.Length);

        public static Task WriteAsync(this Stream stream, Space<byte> span)
            => stream.WriteAsync(span, CancellationToken.None);

        public static Task WriteAsync(this Stream stream, Space<byte> span, CancellationToken cancellationToken)
            => stream.WriteAsync(span.Array, span.Offset, span.Length, cancellationToken);

        public static Task Write(this Stream stream, Space<byte> span)
            => stream.WriteAsync(span.Array, span.Offset, span.Length);
    }
}
