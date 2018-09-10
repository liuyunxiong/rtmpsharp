using System;
using System.IO;
using System.Threading.Tasks;
using Hina;
using Hina.IO;
using Hina.Security;
using RtmpSharp.IO;

namespace RtmpSharp.Net
{
    partial class RtmpClient
    {
        static class Handshake
        {
            public static async Task GoAsync(Stream stream)
            {
                var c1 = await WriteC1Async(stream);
                var s1 = await ReadS1Async(stream);

                if (s1.zero != 0 || s1.three != 3)
                    throw InvalidHandshakeException();

                await WriteC2Async(stream, s1.time, s1.random);
                var s2 = await ReadS2Async(stream);

                if (c1.time != s2.echoTime || !ByteSpaceComparer.IsEqual(c1.random, s2.echoRandom))
                    throw InvalidHandshakeException();
            }

            static Exception InvalidHandshakeException() => throw new ArgumentException("remote server failed the rtmp handshake");



            // "c1" and "s1" are actually a concatenation of c0 and c1. as described in the spec, we can send and receive them together.
            const int C1Length     = FrameLength + 1;
            const int FrameLength  = RandomLength + 4 + 4;
            const int RandomLength = 1528;

            static readonly SerializationContext EmptyContext = new SerializationContext();

            static async Task<(uint time, Space<byte> random)> WriteC1Async(Stream stream)
            {
                var writer = new AmfWriter(new byte[C1Length], EmptyContext);
                var random = RandomEx.GetBytes(RandomLength);
                var time   = Ts.CurrentTime;

                writer.WriteByte(3);       // rtmp version (constant 3) [c0]
                writer.WriteUInt32(time);  // time                      [c1]
                writer.WriteUInt32(0);     // zero                      [c1]
                writer.WriteBytes(random); // random bytes              [c1]

                await stream.WriteAsync(writer.Span);
                writer.Return();

                return (time, random);
            }

            static async Task<(uint three, uint time, uint zero, Space<byte> random)> ReadS1Async(Stream stream)
            {
                var buffer = await stream.ReadBytesAsync(C1Length);
                var reader = new AmfReader(buffer, EmptyContext);

                var three  = reader.ReadByte();             // rtmp version (constant 3) [s0]
                var time   = reader.ReadUInt32();           // time                      [s1]
                var zero   = reader.ReadUInt32();           // zero                      [s1]
                var random = reader.ReadSpan(RandomLength); // random bytes              [s1]

                return (three, time, zero, random);
            }

            static async Task WriteC2Async(Stream stream, uint remoteTime, Space<byte> remoteRandom)
            {
                var time = Ts.CurrentTime;

                var writer = new AmfWriter(new byte[FrameLength], EmptyContext);
                writer.WriteUInt32(remoteTime);  // "time":        a copy of s1 time
                writer.WriteUInt32(time);        // "time2":       current local time
                writer.WriteBytes(remoteRandom); // "random echo": a copy of s1 random

                await stream.WriteAsync(writer.Span);
                writer.Return();
            }

            static async Task<(uint echoTime, Space<byte> echoRandom)> ReadS2Async(Stream stream)
            {
                var buffer = await stream.ReadBytesAsync(FrameLength);
                var reader = new AmfReader(buffer, EmptyContext);

                var time   = reader.ReadUInt32();           // "time":        a copy of c1 time
                var ____   = reader.ReadUInt32();           // "time2":       current local time
                var echo   = reader.ReadSpan(RandomLength); // "random echo": a copy of c1 random

                return (time, echo);
            }
        }
    }
}
