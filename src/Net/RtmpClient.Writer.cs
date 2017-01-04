using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hina;
using Hina.Collections;
using Hina.Threading;
using Konseki;
using RtmpSharp.IO;
using RtmpSharp.Net.Messages;

namespace RtmpSharp.Net
{
    partial class RtmpClient
    {
        class Writer
        {
            readonly RtmpClient              owner;
            readonly CancellationToken       token;
            readonly AsyncAutoResetEvent     reset;
            readonly ConcurrentQueue<Packet> queue;
            readonly Stream                  stream;
            readonly SerializationContext    context;

            // all current chunk streams, keyed by chunk stream id. though the rtmp spec allows for many message
            // streams in each chunk stream, we only ever use one message stream for each chunk stream (no qos is
            // currently implemented).
            readonly IDictionary<int, ChunkStream.Snapshot> streams;

            // the current chunk length for this upstream connection. by the rtmp spec, this defaults to 128 bytes.
            int chunkLength = 128;


            public Writer(RtmpClient owner, Stream stream, SerializationContext context, CancellationToken cancellationToken)
            {
                this.owner   = owner;
                this.stream  = stream;
                this.context = context;

                this.token   = cancellationToken;
                this.reset   = new AsyncAutoResetEvent();
                this.queue   = new ConcurrentQueue<Packet>();
                this.streams = new KeyDictionary<int, ChunkStream.Snapshot>();
            }


            public void QueueWrite(RtmpMessage message, int chunkStreamId, bool external = true)
            {
                // we save ourselves from synchronizing on chunk length because we never modify it post-initialization
                if (external && message is ChunkLength)
                    throw new InvalidOperationException("cannot modify chunk length after stream has begun");

                queue.Enqueue(new Packet(chunkStreamId, message.ContentType, Serialize(message)));
                reset.Set();
            }


            // this method must only be called once
            public async Task RunAsync(int chunkLength)
            {
                if (chunkLength != 0)
                {
                    QueueWrite(new ChunkLength(chunkLength), chunkStreamId: 2, external: false);
                    this.chunkLength = chunkLength;
                }

                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await WriteOnceAsync();
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        Kon.DebugException("rtmpclient::writer encountered an error", e);

                        owner.InternalCloseConnection("writer-exception", e);
                        return;
                    }
                }
            }

            async Task WriteOnceAsync()
            {
                await reset.WaitAsync();

                while (!token.IsCancellationRequested && queue.TryDequeue(out var packet))
                {
                    // quickly estimate the maximum required length for this message. our estimation is as follows:
                    //
                    //     - [payload]
                    //         - take the message length, and add the chunk + message headers.
                    //
                    //     - [chunk headers]
                    //         - all chunk headers begin with a 0-3 byte header, indicating chunk stream id + message header
                    //             format.
                    //         - the one byte variant can encode chunk stream ids up to and including #63. we don't expect to
                    //             encode that many streams right now (unless a user library wants it), so we can assume 1 byte
                    //             chunk headers for now.
                    //
                    //     - [message headers]
                    //         - the first message header must be a type 0 (new) header, which is 11 bytes large.
                    //         - all further message headers can be a type 3 (continuation) header, which is 0 bytes large.
                    //
                    //     - [total]
                    //         - message_length + chunk_count * 1 + 11
                    //
                    var packetLength       = packet.Span.Length;
                    var chunkCount         = packetLength / chunkLength + 1;
                    var estimatedMaxLength = packetLength + chunkCount + 11;
                    var writer             = new AmfWriter(estimatedMaxLength, context);

                    var previous           = streams.GetDefault(packet.StreamId);
                    var next               = previous.Clone();
                    next.Ready             = true;
                    next.ContentType       = packet.Type;
                    next.ChunkStreamId     = packet.StreamId;
                    next.MessageStreamId   = 0;
                    next.MessageLength     = packetLength;
                    next.Timestamp         = Ts.CurrentTime;

                    streams[packet.StreamId] = next;
                    ChunkStream.WriteTo(writer, previous, next, chunkLength, packet.Span);

                    await stream.WriteAsync(writer.Span, token);
                    writer.Return();
                }
            }

            Space<byte> Serialize(RtmpMessage message)
            {
                // (this comment must be kept in sync at rtmpclient.reader.cs and rtmpclient.writer.cs)
                //
                // unsupported type summary:
                //
                // - aggregate:      we have never encountered this packet in the wild
                // - shared objects: we have not found a use case for this
                // - data commands:  we have not found a use case for this, though it should be extremely easy to
                //                       support. it's just a one-way equivalent of command (invoke). that is, we don't
                //                       generate an invoke id for it, and it does not contain headers. other than that,
                //                       they're identical. we can use existing logic and add if statements to surround
                //                       writing the invokeid + headers if needed.

                const int WriteInitialBufferLength = 4192;

                var w = new AmfWriter(WriteInitialBufferLength, context);

                switch (message.ContentType)
                {
                    case PacketContentType.SetChunkSize:
                        var a = (ChunkLength)message;
                        w.WriteInt32(a.Length);
                        break;

                    case PacketContentType.AbortMessage:
                        var b = (Abort)message;
                        w.WriteInt32(b.ChunkStreamId);
                        break;

                    case PacketContentType.Acknowledgement:
                        var c = (Acknowledgement)message;
                        w.WriteUInt32(c.TotalRead);
                        break;

                    case PacketContentType.UserControlMessage:
                        var d = (UserControlMessage)message;

                        w.WriteUInt16((ushort)d.EventType);
                        foreach (var value in d.Values)
                            w.WriteUInt32(value);

                        break;

                    case PacketContentType.WindowAcknowledgementSize:
                        var e = (WindowAcknowledgementSize)message;
                        w.WriteInt32(e.Count);
                        break;

                    case PacketContentType.SetPeerBandwith:
                        var f = (PeerBandwidth)message;
                        w.WriteInt32(f.AckWindowSize);
                        w.WriteByte((byte)f.LimitType);
                        break;

                    case PacketContentType.Audio:
                    case PacketContentType.Video:
                        var g = (ByteData)message;
                        w.WriteBytes(g.Data);
                        break;

                    case PacketContentType.DataAmf0:
                        throw NotSupportedException("data-amf0");

                    case PacketContentType.SharedObjectAmf0:
                        throw NotSupportedException("sharedobject-amf0");

                    case PacketContentType.CommandAmf0:
                        WriteCommand(ObjectEncoding.Amf0, w, message);
                        break;

                    case PacketContentType.DataAmf3:
                        throw NotSupportedException("data-amf3");

                    case PacketContentType.SharedObjectAmf3:
                        throw NotSupportedException("sharedobject-amf0");

                    case PacketContentType.CommandAmf3:
                        // first byte is an encoding specifier byte.
                        //     see `writecommand` comment below: specify amf0 object encoding and elevate into amf3.
                        w.WriteByte((byte)ObjectEncoding.Amf0);
                        WriteCommand(ObjectEncoding.Amf3, w, message);
                        break;

                    case PacketContentType.Aggregate:
                        throw NotSupportedException("aggregate");

                    default:
                        throw NotSupportedException($"unknown ({message.ContentType})");
                }

                return w.Span;
            }

            // most rtmp servers we are interested in only support amf3 via an amf0 envelope
            static void WriteCommand(ObjectEncoding encoding, AmfWriter w, RtmpMessage message)
            {
                switch (message)
                {
                    case Notify notify:
                        w.WriteBoxedAmf0Object(encoding, notify.Data);
                        break;

                    case Invoke request:
                        w.WriteBoxedAmf0Object(encoding, request.MethodName);
                        w.WriteBoxedAmf0Object(encoding, request.InvokeId);
                        w.WriteBoxedAmf0Object(encoding, request.Headers);

                        foreach (var arg in request.Arguments ?? EmptyArray<object>.Instance)
                            w.WriteBoxedAmf0Object(encoding, arg);

                        break;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }

            struct Packet
            {
                // this is the chunk stream id. as above, we only ever use one message stream per chunk stream.
                public int               StreamId;
                public Space<byte>       Span;
                public PacketContentType Type;

                public Packet(int streamId, PacketContentType type, Space<byte> span)
                {
                    StreamId = streamId;
                    Type = type;
                    Span = span;
                }
            }

            static Exception NotSupportedException(string type) => new NotSupportedException($"packets with the type \"{type}\" aren't supported right now.");
        }
    }
}
