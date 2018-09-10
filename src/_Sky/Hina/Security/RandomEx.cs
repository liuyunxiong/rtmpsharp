using System;
using System.Security.Cryptography;
using System.Text;

// csharp: hina/security/randomex.cs [snipped]
namespace Hina.Security
{
    // a cryptographically secure random number generator.
    class RandomEx
    {
        const int BufferLength     = 4096;
        const int HalfBufferLength = BufferLength / 2;

        public static readonly string LowerCaseAlphanumeric = "abcdefghijklmnopqrstuvwxyz0123456789";
        public static readonly string LowerCaseAlphabet     = "abcdefghijklmnopqrstuvwxyz";

        static readonly RandomNumberGenerator Random = RandomNumberGenerator.Create();
        static readonly object SyncObject = new object();

        static byte[] buffer   = new byte[BufferLength];
        static int    position = 0;


        static RandomEx()
        {
            RefillBuffer();
        }


        static uint GetUInt32()
        {
            lock (SyncObject)
            {
                RequireData(4);

                var number = BitConverter.ToUInt32(buffer, position);
                position += 4;

                return number;
            }
        }

        // returns a positive random integer
        public static int GetInt32()
        {
            return (int)GetUInt32() & 0x7FFFFFFF;
        }

        // returns a random number in the range [0, maxValue)
        public static int GetInt32(int maxValue)
        {
            if (maxValue < 0)
                throw new ArgumentOutOfRangeException(nameof(maxValue));

            return GetInt32(0, maxValue);
        }

        // returns a random number in the range [minValue, maxValue)
        public static int GetInt32(int minValue, int maxValue)
        {
            if (minValue > maxValue)
                throw new ArgumentOutOfRangeException(nameof(minValue));

            if (minValue == maxValue)
                return minValue;

            var difference = maxValue - minValue;

            while (true)
            {
                var number = GetUInt32();

                var max = (long)uint.MaxValue + 1;
                var remainder = max % difference;

                if (max - remainder > number)
                    return (int)(minValue + number % difference);
            }
        }

        public static byte[] GetBytes(int size)
        {
            var data = new byte[size];
            GetBytes(data);
            return data;
        }

        public static void GetBytes(byte[] destination)
        {
            Check.NotNull(destination);

            lock (SyncObject)
            {
                // draw from the buffer, if it is less than half our buffer length. we arbitrarily
                // choose half to prevent carcinogenic conditions where we have a buffer length of
                // `n` bytes, and the caller continually requests `n - 1` bytes, causing us to
                // refill the buffer every call, when the purpose of the buffer is to reduce
                // the p/invoke penalty for small requests
                if (destination.Length <= HalfBufferLength)
                {
                    var count = destination.Length;

                    RequireData(count);
                    Buffer.BlockCopy(buffer, position, destination, 0, count);

                    position += count;
                    return;
                }

                Random.GetBytes(destination);
            }
        }

        // returns a random double in the range [0.0, 1.0)
        public static double GetDouble()
        {
            return GetUInt32() / (1.0 + uint.MaxValue);
        }

        public static string GetString(int length)
        {
            return GetString(LowerCaseAlphanumeric, length);
        }

        public static string GetString(string alphabet, int length)
        {
            var builder = new StringBuilder(length);
            var alphabetLength = alphabet.Length;

            for (var i = 0; i < length; i++)
            {
                var index = GetInt32(alphabetLength);
                builder.Append(alphabet[index]);
            }

            return builder.ToString();
        }

        static void RefillBuffer()
        {
            if (buffer == null)
                buffer = new byte[BufferLength];

            Random.GetBytes(buffer);
            position = 0;
        }

        static void RequireData(int bytes)
        {
            if (bytes > buffer.Length)
                throw new ArgumentOutOfRangeException(nameof(bytes), "requesting more bytes than buffer can hold");

            if (bytes > BufferLength - position)
                RefillBuffer();
        }
    }
}
