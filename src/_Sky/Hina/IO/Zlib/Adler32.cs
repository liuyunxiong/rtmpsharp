// csharp: hina/io/zlib/adler32.cs [snipped]
namespace Hina.IO.Zlib
{
    // implementation adapted from zlib's `adler32.c` (zlib 0.71)
    // api inspired by openjdk's `java.util.zip.Adler32`
    public class Adler32
    {
        const int Modulo = 65521;
        const int NMax = 5552;

        uint checksum = 1;

        public int Checksum => (int)checksum;

        public void Update(byte[] buffer, int offset, int count)
        {
            CheckDebug.NotNull(buffer);

            var s1 = checksum & 0xFFFF;
            var s2 = (checksum >> 16) & 0xFFFF;
            var index = offset;

            while (count > 0)
            {
                var k = count < NMax ? count : NMax;
                count -= k;

                for (var i = 0; i < k; i++)
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
            CheckDebug.NotNull(buffer);

            Update(buffer, 0, buffer.Length);
        }
    }
}
