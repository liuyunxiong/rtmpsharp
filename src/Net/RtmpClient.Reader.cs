using System;
using System.Buffers;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Hina;
using Hina.Collections;
using Hina.Linq;
using Hina.Threading;
using Konseki;
using RtmpSharp.IO;
using RtmpSharp.Net.Messages;

namespace RtmpSharp.Net
{
    partial class RtmpClient
    {
        class Reader
        {
            const int DefaultBufferLength = 4192;

            readonly RtmpClient               owner;
            readonly CancellationToken        token;
            readonly AsyncAutoResetEvent      reset;
            readonly ConcurrentQueue<Builder> queue;
            readonly Stream                   stream;
            readonly SerializationContext     context;

            // an intermediate processing buffer of data read from `stream`. this is always at least `chunkLength` bytes large.
            byte[] buffer;

            // the number of bytes available in `buffer`
            int available;

            // all current chunk streams, keyed by chunk stream id
            readonly IDictionary<int, ChunkStream.Snapshot> streams;

            // all current message streams
            readonly IDictionary<(int chunkStreamId, uint messageStreamId), Builder> messages;

            // the current chunk length for this stream. by the rtmp spec, this defaults to 128 bytes.
            int chunkLength = 128;

            // the current acknowledgement window for this stream. after we receive more than `acknowledgementLength`
            // bytes, we must send an acknowledgement back to the remote peer.
            int acknowledgementLength = 0;

            // tracking counter for the number of bytes we've received, in order to send acknowledgements.
            long readTotal = 0;
            long readSinceLastAcknowledgement = 0;

            // cached amfreaders so that we do not pay the cost of allocation on every payload or message. these are
            // exclusively owned by their respective methods.
            readonly AmfReader __readFramesFromBufferReader;
            readonly AmfReader __readSingleFrameReader;


            public Reader(RtmpClient owner, Stream stream, SerializationContext context, CancellationToken cancellationToken)
            {
                this.owner     = owner;
                this.stream    = stream;
                this.context   = context;
                this.token     = cancellationToken;

                this.reset     = new AsyncAutoResetEvent();
                this.queue     = new ConcurrentQueue<Builder>();
                this.streams   = new KeyDictionary<int, ChunkStream.Snapshot>();
                this.messages  = new KeyDictionary<(int, uint), Builder>();

                this.buffer    = new byte[DefaultBufferLength];
                this.available = 0;

                this.__readSingleFrameReader = new AmfReader(context);
                this.__readFramesFromBufferReader = new AmfReader(context);
            }


            // this method must only be called once
            public async Task RunAsync()
            {
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        await ReadOnceAsync();
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }
                    catch (Exception e)
                    {
                        Kon.DebugException("rtmpclient::reader encountered an error", e);

                        owner.InternalCloseConnection("reader-exception", e);
                        return;
                    }
                }
            }

            async Task ReadOnceAsync()
            {
                // read a bunch of bytes from the remote server
                await ReadFromStreamAsync();

                // send an acknowledgement if we need it
                MaybeSendAcknowledgements();

                // then, read all frames, chunks and messages that are complete within it
                ReadFramesFromBuffer();
            }

            void MaybeSendAcknowledgements()
            {
                if (acknowledgementLength == 0)
                    return;

                while (readSinceLastAcknowledgement >= acknowledgementLength)
                {
                    readSinceLastAcknowledgement -= acknowledgementLength;

                    var ack = new Acknowledgement((uint)(readTotal - readSinceLastAcknowledgement));
                    owner.queue(ack, 2);
                }
            }

            async Task ReadFromStreamAsync()
            {
                var read = await stream.ReadAsync(new Space<byte>(buffer, available), token);

                if (read == 0)
                    throw new EndOfStreamException("rtmp connection was closed by the remote peer");

                available += read;
                readTotal += read;
                readSinceLastAcknowledgement += read;
            }

            unsafe void ReadFramesFromBuffer()
            {
                // the index that we have successfully read into `buffer`. that is, all bytes before this have been
                // successfully read and processed into a valid packet.
                var index  = 0;
                var reader = __readFramesFromBufferReader;

                // read as many frames as we can from the buffer
                while (index < available)
                {
                    reader.Rebind(buffer, index, available - index);

                    if (!ReadSingleFrame(reader))
                        break;

                    index += reader.Position;
                }

                // then, shift unread bytes back to the start of the array
                if (index > 0)
                {
                    if (available != index)
                    {
                        fixed (byte* pSource      = &buffer[index])
                        fixed (byte* pDestination = &buffer[0])
                        {
                            Buffer.MemoryCopy(
                                source:                 pSource,
                                destination:            pDestination,
                                destinationSizeInBytes: buffer.Length,
                                sourceBytesToCopy:      available - index);
                        }
                    }

                    available -= index;
                }
            }

            bool ReadSingleFrame(AmfReader reader)
            {
                if (!ChunkStream.ReadFrom1(reader, out var streamId, out var opaque))
                    return false;

                if (!streams.TryGetValue(streamId, out var previous))
                    previous = new ChunkStream.Snapshot() { ChunkStreamId = streamId };

                if (!ChunkStream.ReadFrom2(reader, previous, opaque, out var next))
                    return false;

                streams[streamId] = next;
                context.RequestReadAllocation(next.MessageLength);

                var key     = (next.ChunkStreamId, next.MessageStreamId);
                var builder = messages.TryGetValue(key, out var packet) ? packet : messages[key] = new Builder(next.MessageLength);
                var length  = Math.Min(chunkLength, builder.Remaining);

                if (!reader.HasLength(length))
                    return false;

                builder.AddData(
                    reader.ReadSpan(length));

                if (builder.Current == builder.Length)
                {
                    messages.Remove(key);

                    using (builder)
                    {
                        var dereader = __readSingleFrameReader;
                        dereader.Rebind(builder.Span);

                        var message = Deserialize(next.ContentType, dereader);
                        DispatchMessage(message);
                    }
                }

                return true;
            }

            void DispatchMessage(RtmpMessage message)
            {
                switch (message)
                {
                    case ChunkLength chunk:
                        Kon.Trace("received: chunk-length", new { length = chunk.Length });

                        if (chunk.Length < 0)
                            throw new ArgumentException("invalid chunk length");

                        context.RequestReadAllocation(chunk.Length);
                        chunkLength = chunk.Length;
                        break;

                    case WindowAcknowledgementSize acknowledgement:
                        if (acknowledgement.Count < 0)
                            throw new ArgumentException("invalid acknowledgement window length");

                        acknowledgementLength = acknowledgement.Count;
                        break;

                    case Abort abort:
                        Kon.Trace("received: abort", new { chunk = abort.ChunkStreamId });

                        // delete the chunk stream
                        streams.Remove(abort.ChunkStreamId);

                        // then, delete all message streams associated with that chunk stream
                        foreach (var (key, builder) in messages.FilterArray(x => x.Key.chunkStreamId == abort.ChunkStreamId))
                        {
                            messages.Remove(key);
                            builder .Dispose();
                        }

                        break;

                    default:
                        owner.InternalReceiveEvent(message);
                        break;
                }
            }

            // todo: refactor
            RtmpMessage Deserialize(PacketContentType contentType, AmfReader r)
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

                switch (contentType)
                {
                    case PacketContentType.SetChunkSize:
                        return new ChunkLength(
                            length: r.ReadInt32());

                    case PacketContentType.AbortMessage:
                        return new Abort(
                            chunkStreamId: r.ReadInt32());

                    case PacketContentType.Acknowledgement:
                        return new Acknowledgement(
                            read: r.ReadUInt32());

                    case PacketContentType.UserControlMessage:
                        var type   = r.ReadUInt16();
                        var values = EnumerableEx.Range(r.Remaining / 4, r.ReadUInt32);

                        return new UserControlMessage(
                            type:   (UserControlMessage.Type)type,
                            values: values);

                    case PacketContentType.WindowAcknowledgementSize:
                        return new WindowAcknowledgementSize(
                            count: r.ReadInt32());

                    case PacketContentType.SetPeerBandwith:
                        return new PeerBandwidth(
                            acknowledgementWindowSize: r.ReadInt32(),
                            type:                 r.ReadByte());

                    case PacketContentType.Audio:
                        return new AudioData(
                            r.ReadBytes(r.Remaining));

                    case PacketContentType.Video:
                        return new VideoData(
                            r.ReadBytes(r.Remaining));

                    case PacketContentType.DataAmf0:
                        throw NotSupportedException("data-amf0");

                    case PacketContentType.SharedObjectAmf0:
                        throw NotSupportedException("sharedobject-amf0");

                    case PacketContentType.CommandAmf0:
                        return ReadCommand(ObjectEncoding.Amf0, contentType, r);

                    case PacketContentType.DataAmf3:
                        throw NotSupportedException("data-amf3");

                    case PacketContentType.SharedObjectAmf3:
                        throw NotSupportedException("sharedobject-amf0");

                    case PacketContentType.CommandAmf3:
                        var encoding = (ObjectEncoding)r.ReadByte();
                        return ReadCommand(encoding, contentType, r);

                    case PacketContentType.Aggregate:
                        throw NotSupportedException("aggregate");

                    default:
                        throw NotSupportedException($"unknown ({contentType})");
                }
            }

            static RtmpMessage ReadCommand(ObjectEncoding encoding, PacketContentType type, AmfReader r)
            {
                var name     = (string)r.ReadAmfObject(encoding);
                var invokeId = Convert.ToUInt32(r.ReadAmfObject(encoding));
                var headers  = r.ReadAmfObject(encoding);

                var args = new List<object>();

                while (r.Remaining > 0)
                    args.Add(r.ReadAmfObject(encoding));

                return new Invoke(type) { MethodName = name, Arguments = args.ToArray(), InvokeId = invokeId, Headers = headers };
            }

            class Builder : IDisposable
            {
                public byte[] Buffer;
                public int    Current;
                public int    Length;
                public int    Remaining => Length - Current;
                public Space<byte> Span => new Space<byte>(Buffer, 0, Length);

                public Builder(int length)
                {
                    Buffer  = ArrayPool<byte>.Shared.Rent(length);
                    Current = 0;
                    Length  = length;
                }

                public void Dispose()
                {
                    ArrayPool<byte>.Shared.Return(Buffer);
                }

                public void AddData(Space<byte> source)
                {
                    if (source.Length > Remaining)
                        throw new ArgumentException("source span would overflow destination");

                    var destination = new Space<byte>(Buffer, Current, source.Length);

                    source.CopyTo(destination);
                    Current += source.Length;
                }
            }

            static Exception NotSupportedException(string type) => new NotSupportedException($"packets with the type \"{type}\" aren't supported right now.");
        }
    }
}
