using System;
using Hina;
using Konseki;
using RtmpSharp.IO;

namespace RtmpSharp.Net
{
    partial class RtmpClient
    {
        // - each rtmp connection carries multiple chunk streams
        // - each chunk stream    carries multiple message streams
        // - each message stream  carries multiple messages

        // chunk format:
        //
        // +--------------+----------------+--------------------+------------+
        // | basic header | message header | extended timestamp | chunk data |
        // +--------------+----------------+--------------------+------------+
        // |                                                    |
        // |<------------------- chunk header ----------------->|

        static class BasicHeader
        {
            public static void WriteTo(AmfWriter writer, byte format, int chunkStreamId)
            {
                if (chunkStreamId <= 63)
                {
                    //  0 1 2 3 4 5 6 7
                    // +-+-+-+-+-+-+-+-+
                    // |fmt|   cs id   |
                    // +-+-+-+-+-+-+-+-+
                    writer.WriteByte((byte)((format << 6) + chunkStreamId));
                }
                else if (chunkStreamId <= 320)
                {
                    //  0             1
                    //  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
                    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    // |fmt|    0     |   cs id - 64   |
                    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    writer.WriteByte((byte)(format << 6));
                    writer.WriteByte((byte)(chunkStreamId - 64));
                }
                else
                {
                    //  0               1               3
                    //  0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7 0 1 2 3 4 5 6 7
                    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    // |fmt|     1    |           cs id - 64           |
                    // +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                    writer.WriteByte((byte)((format << 6) | 1));
                    writer.WriteByte((byte)((chunkStreamId - 64) & 0xff));
                    writer.WriteByte((byte)((chunkStreamId - 64) >> 8));
                }
            }

            public static bool ReadFrom(AmfReader reader, out int format, out int chunkStreamId)
            {
                format = 0;
                chunkStreamId = 0;

                if (!reader.HasLength(1))
                    return false;

                var b0  = reader.ReadByte();
                var v   = b0 & 0x3f;
                var fmt = b0 >> 6;

                switch (v)
                {
                    // 2 byte variant
                    case 0:
                        if (!reader.HasLength(1))
                            return false;

                        format = fmt;
                        chunkStreamId = reader.ReadByte() + 64;
                        return true;

                    // 3 byte variant
                    case 1:
                        if (!reader.HasLength(2))
                            return false;

                        format = fmt;
                        chunkStreamId = reader.ReadByte() + reader.ReadByte() * 256 + 64;
                        return true;

                    // 1 byte variant
                    default:
                        format = fmt;
                        chunkStreamId = v;
                        return true;
                }
            }
        }

        static class MessageHeader
        {
            const uint ExtendedTimestampSentinel = 0xFFFFFF;

            public static void WriteTo(AmfWriter writer, Type type, ChunkStream.Snapshot stream)
            {
                var extendTs = stream.Timestamp >= ExtendedTimestampSentinel;
                var inlineTs = extendTs ? ExtendedTimestampSentinel : stream.Timestamp;

                switch (type)
                {
                    case Type.Type0:
                        writer.WriteUInt24((uint)inlineTs);
                        writer.WriteUInt24((uint)stream.MessageLength);
                        writer.WriteByte  ((byte)stream.ContentType);
                        writer.WriteLittleEndianInt(stream.MessageStreamId);
                        if (extendTs) writer.WriteUInt32(stream.Timestamp);
                        break;

                    case Type.Type1:
                        writer.WriteUInt24((uint)inlineTs);
                        writer.WriteUInt24((uint)stream.MessageLength);
                        writer.WriteByte  ((byte)stream.ContentType);
                        if (extendTs) writer.WriteUInt32(stream.Timestamp);
                        break;

                    case Type.Type2:
                        writer.WriteUInt24((uint)inlineTs);
                        if (extendTs) writer.WriteUInt32(stream.Timestamp);
                        break;

                    case Type.Type3:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(type));
                }
            }

            public static bool ReadFrom(AmfReader reader, Type type, ChunkStream.Snapshot previous, out ChunkStream.Snapshot next)
            {
                next               = default(ChunkStream.Snapshot);
                next.Ready         = true;
                next.ChunkStreamId = previous.ChunkStreamId;

                if (!reader.HasLength(TypeByteLengths[(byte)type]))
                    return false;

                switch (type)
                {
                    case Type.Type0:
                        next.Timestamp       = reader.ReadUInt24();
                        next.MessageLength   = (int)reader.ReadUInt24();
                        next.ContentType     = (PacketContentType)reader.ReadByte();
                        next.MessageStreamId = (uint)reader.ReadLittleEndianInt();

                        return MaybeReadExtraTimestamp(ref next.Timestamp);

                    case Type.Type1:
                        next.Timestamp       = reader.ReadUInt24();
                        next.MessageLength   = (int)reader.ReadUInt24();
                        next.ContentType     = (PacketContentType)reader.ReadByte();

                        next.MessageStreamId = previous.MessageStreamId;
                        return MaybeReadExtraTimestamp(ref next.Timestamp);

                    case Type.Type2:
                        next.Timestamp       = reader.ReadUInt24();

                        next.MessageLength   = previous.MessageLength;
                        next.ContentType     = previous.ContentType;
                        next.MessageStreamId = previous.MessageStreamId;
                        return MaybeReadExtraTimestamp(ref next.Timestamp);

                    case Type.Type3:
                        next.Timestamp       = previous.Timestamp;
                        next.MessageLength   = previous.MessageLength;
                        next.ContentType     = previous.ContentType;
                        next.MessageStreamId = previous.MessageStreamId;
                        return true;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(type), "unknown type");
                }

                bool MaybeReadExtraTimestamp(ref uint timestamp)
                {
                    if (timestamp != ExtendedTimestampSentinel)
                        return true;

                    if (!reader.HasLength(4))
                        return false;

                    timestamp = (uint)reader.ReadInt32();
                    return true;
                }
            }

            // byte lengths for the message headers. if timestamp indicates an extended timestamp, then add an extra 4
            // bytes to this value.
            static readonly int[] TypeByteLengths = { 11, 7, 3, 0 };

            public enum Type : byte
            {
                // all fields are included
                Type0 = 0,

                // timestamp delta + message length + message type id only. assumes the same message stream id as previous.
                Type1 = 1,

                // timestamp delta only. assumes the same message stream id and length as previous. ideal for
                // constant-sized media.
                Type2 = 2,

                // no values. assumes the same values (including timestamp) as previous.
                Type3 = 3
            }
        }

        static class ChunkStream
        {
            public static void WriteTo(AmfWriter writer, Snapshot previous, Snapshot next, int chunkLength, Space<byte> message)
            {
                Kon.Assert(
                    !previous.Ready || previous.ChunkStreamId == next.ChunkStreamId,
                    "previous and next describe two different chunk streams");

                Kon.Assert(
                    next.MessageLength == message.Length,
                    "mismatch between reported message length and actual message length");

                // we don't write zero-length packets, and as state for `next` and `previous` won't match what our peer
                // sees if we pass a zero-length message here. zero-length sends should be filtered out at a higher level.
                Kon.Assert(
                    next.MessageLength != 0,
                    "message length cannot be zero");


                var header = GetInitialHeaderType();

                for (var i = 0; i < next.MessageLength; i += chunkLength)
                {
                    if (i == 0)
                    {
                        BasicHeader  .WriteTo(writer, (byte)header, next.ChunkStreamId);
                        MessageHeader.WriteTo(writer, header, next);
                    }
                    else
                    {
                        BasicHeader.WriteTo(writer, (byte)MessageHeader.Type.Type3, next.ChunkStreamId);
                    }

                    var count = Math.Min(chunkLength, next.MessageLength - i);
                    var slice = message.Slice(i, count);

                    writer.WriteBytes(slice);
                }


                MessageHeader.Type GetInitialHeaderType()
                {
                    if (!previous.Ready || next.MessageStreamId != previous.MessageStreamId)
                        return MessageHeader.Type.Type0;

                    if (next.MessageLength != previous.MessageLength || next.ContentType != previous.ContentType)
                        return MessageHeader.Type.Type1;

                    if (next.Timestamp != previous.Timestamp)
                        return MessageHeader.Type.Type2;

                    return MessageHeader.Type.Type3;
                }
            }

            // part 1: read from the chunk stream, returning true if enough data is here. you must take the value
            // returned at `chunkStreamId`, find the chunk stream snapshot associated with that chunk stream and
            // pass it to the second stage (part2) along with the opaque value.
            public static bool ReadFrom1(AmfReader reader, out int chunkStreamId, out MessageHeader.Type opaque)
            {
                if (BasicHeader.ReadFrom(reader, out var format, out var streamId))
                {
                    opaque        = (MessageHeader.Type)format;
                    chunkStreamId = streamId;
                    return true;
                }
                else
                {
                    opaque        = default(MessageHeader.Type);
                    chunkStreamId = default(int);
                    return false;
                }
            }

            public static bool ReadFrom2(AmfReader reader, Snapshot previous, MessageHeader.Type opaque, out Snapshot next)
            {
                return MessageHeader.ReadFrom(reader, opaque, previous, out next);
            }


            // a point-in-time snapshot of some chunk stream, including the currently packet in transit
            public struct Snapshot
            {
                // if false, the value of this object is semantically equivalent to `null`
                public bool Ready;


                // * * * * * * * * * *
                // chunk stream values

                // the "chunk stream id" for this chunk stream
                public int ChunkStreamId;

                // the current timestamp for this chunk stream
                public uint Timestamp;


                // * * * * * * * * * *
                // message stream values

                // message stream id
                public uint MessageStreamId;


                // * * * * * * * * * *
                // current message values

                // the "message type" for the current packet
                public PacketContentType ContentType;

                // size of the last chunk, in bytes, of the message currently being transmitted
                public int MessageLength;

                
                // * * * * * * * * * *
                // methods

                public Snapshot Clone() => (Snapshot)MemberwiseClone();
            }
        }
    }

    static class Ts
    {
        // because we do not currently transmit audio or video packets, or any kind of data that requires a per-chunk
        // timestamp, can get away without timestamps at all. however, we may need this in the future should we decide
        // to support them.
        public static uint CurrentTime => 0;
    }
}