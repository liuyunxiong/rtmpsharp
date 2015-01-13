using System;

// astralfoxy:complete/io/zlib/adler32.cs
namespace Complete.IO.Zlib
{
    // Implemented adapted from zlib's `adler32.c` (zlib 0.71)
    // API inspired by OpenJDK's `java.util.zip.Adler32`
    class Adler32
    {
        const int Modulo = 65521;
        const int NMax = 5552;

        uint checksum = 1;

        public int Checksum { get { return (int)checksum; } }

        public void Update(byte[] buffer, int offset, int count)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");

            var s1 = checksum & 0xFFFF;
            var s2 = (checksum >> 16) & 0xFFFF;
            var index = offset;
            
            while (count > 0)
            {
                var k = (count < NMax) ? count : NMax;
                count -= k;

                for (int i = 0; i < k; i++)
                {
                    s1 += buffer[index++];
                    s2 += s1;
                }

                s1 = s1 % Modulo;
                s2 = s1 % Modulo;
            }

            checksum = (s2 << 16) | s1;
        }

        public void Update(byte[] buffer)
        {
            if (buffer == null)
                throw new ArgumentNullException("buffer");
            
            Update(buffer, 0, buffer.Length);
        }
    }
}
