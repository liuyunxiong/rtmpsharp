using System;
using System.IO;

// csharp: hina/io/extensions/memorystreamextensions.cs [snipped]
namespace Hina.IO
{
    public static class MemoryStreamExtensions
    {
        public static ArraySegment<byte> GetBuffer(this MemoryStream stream)
        {
            if (stream.TryGetBuffer(out var buffer))
                return buffer;

            throw new ArgumentException("failed to extract underlying buffer from memory stream");
        }
    }
}
