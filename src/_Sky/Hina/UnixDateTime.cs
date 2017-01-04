using System;

// csharp: hina/bytespacecomparer.cs [snipped]
namespace Hina
{
    static class UnixDateTime
    {
        public static readonly DateTime Epoch = DateTimeEx.CreateUtc(1970);

        public static DateTime FromUnix(long seconds) => Epoch.AddSeconds(seconds);
        public static long     ToUnix(DateTime date)  => (long)(date - Epoch).TotalSeconds;

        public static DateTime FromUnixMilliseconds(long seconds) => Epoch.AddMilliseconds(seconds);
        public static long     ToUnixMilliseconds(DateTime date)  => (long)(date - Epoch).TotalMilliseconds;
    }
}
