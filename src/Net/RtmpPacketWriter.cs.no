using Complete;
using RtmpSharp.IO;
using RtmpSharp.Messaging;
using RtmpSharp.Messaging.Events;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;

namespace RtmpSharp.Net
{
    // has noop write scheduling property until we implement fair writing
    // shared objects not implemented (yet)
    class RtmpPacketWriter
    {
        public bool Continue { get; set; }

        public event EventHandler<ExceptionalEventArgs> Disconnected;

        readonly AmfWriter writer;
        readonly Dictionary<int, RtmpHeader> rtmpHeaders;
        readonly ConcurrentQueue<RtmpPacket> queuedPackets;
        readonly AutoResetEvent packetAvailableEvent;
        readonly ObjectEncoding objectEncoding;

        // defined by the spec
        const int DefaultChunkSize = 128;
        int writeChunkSize = DefaultChunkSize;

        public RtmpPacketWriter(AmfWriter writer, ObjectEncoding objectEncoding)
        {
            this.objectEncoding = objectEncoding;
            this.writer = writer;

            rtmpHeaders = new Dictionary<int, RtmpHeader>();
            queuedPackets = new ConcurrentQueue<RtmpPacket>();
            packetAvailableEvent = new AutoResetEvent(false);

            Continue = true;
        }

        void OnDisconnected(ExceptionalEventArgs e)
        {
            Continue = false;

            if (Disconnected != null)
                Disconnected(this, e);
        }

        public void WriteLoop()
        {
            try
            {
                while (Continue)
                {
                    packetAvailableEvent.WaitOne();
                    RtmpPacket packet;
                    while (queuedPackets.TryDequeue(out packet))
                        WritePacket(packet);
                }
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.Print("Exception: {0} at {1}", ex, ex.StackTrace);
                if (ex.InnerException != null)
                {
                    var inner = ex.InnerException;
                    System.Diagnostics.Debug.Print("InnerException: {0} at {1}", inner, inner.StackTrace);
                }
#endif

                OnDisconnected(new ExceptionalEventArgs("rtmp-packet-writer", ex));
            }
        }

        static ChunkMessageHeaderType GetMessageHeaderType(RtmpHeader header, RtmpHeader previousHeader)
        {
            if (previousHeader == null || header.MessageStreamId != previousHeader.MessageStreamId || !header.IsTimerRelative)
                return ChunkMessageHeaderType.New;

            if (header.PacketLength != previousHeader.PacketLength || header.MessageType != previousHeader.MessageType)
                return ChunkMessageHeaderType.SameSource;

            if (header.Timestamp != previousHeader.Timestamp)
                return ChunkMessageHeaderType.TimestampAdjustment;

            return ChunkMessageHeaderType.Continuation;
        }

        public void Queue(RtmpEvent message, int streamId, int messageStreamId)
        {
            var header = new RtmpHeader();
            var packet = new RtmpPacket(header, message);

            header.StreamId = streamId;
            header.Timestamp = message.Timestamp;
            header.MessageStreamId = messageStreamId;
            header.MessageType = message.MessageType;
            if (message.Header != null)
                header.IsTimerRelative = message.Header.IsTimerRelative;
            queuedPackets.Enqueue(packet);
            packetAvailableEvent.Set();
        }

        static int GetBasicHeaderLength(int streamId)
        {
            if (streamId >= 320)
                return 3;
            if (streamId >= 64)
                return 2;
            return 1;
        }

        void WritePacket(RtmpPacket packet)
        {
            var header = packet.Header;
            var streamId = header.StreamId;
            var message = packet.Body;

            var buffer = GetMessageBytes(header, message);
            header.PacketLength = buffer.Length;

            RtmpHeader previousHeader;
            rtmpHeaders.TryGetValue(streamId, out previousHeader);

            rtmpHeaders[streamId] = header;

            WriteMessageHeader(header, previousHeader);

            var first = true;
            for (var i = 0; i < header.PacketLength; i += writeChunkSize)
            {
                if (!first)
                    WriteBasicHeader(ChunkMessageHeaderType.Continuation, header.StreamId);

                var bytesToWrite = i + writeChunkSize > header.PacketLength ? header.PacketLength - i : writeChunkSize;
                writer.Write(buffer, i, bytesToWrite);
                first = false;
            }

            var chunkSizeMsg = message as ChunkSize;
            if (chunkSizeMsg != null)
                writeChunkSize = chunkSizeMsg.Size;
        }

        void WriteBasicHeader(ChunkMessageHeaderType messageHeaderFormat, int streamId)
        {
            var fmt = (byte)messageHeaderFormat;
            if (streamId <= 63)
            {
                writer.WriteByte((byte)((fmt << 6) + streamId));
            }
            else if (streamId <= 320)
            {
                writer.WriteByte((byte)(fmt << 6));
                writer.WriteByte((byte)(streamId - 64));
            }
            else
            {
                writer.WriteByte((byte)((fmt << 6) | 1));
                writer.WriteByte((byte)((streamId - 64) & 0xff));
                writer.WriteByte((byte)((streamId - 64) >> 8));
            }
        }

        void WriteMessageHeader(RtmpHeader header, RtmpHeader previousHeader)
        {
            var headerType = GetMessageHeaderType(header, previousHeader);
            WriteBasicHeader(headerType, header.StreamId);

            var uint24Timestamp = header.Timestamp < 0xFFFFFF ? header.Timestamp : 0xFFFFFF;
            switch (headerType)
            {
                case ChunkMessageHeaderType.New:
                    writer.WriteUInt24(uint24Timestamp);
                    writer.WriteUInt24(header.PacketLength);
                    writer.WriteByte((byte)header.MessageType);
                    writer.WriteReverseInt(header.MessageStreamId);
                    break;
                case ChunkMessageHeaderType.SameSource:
                    writer.WriteUInt24(uint24Timestamp);
                    writer.WriteUInt24(header.PacketLength);
                    writer.WriteByte((byte)header.MessageType);
                    break;
                case ChunkMessageHeaderType.TimestampAdjustment:
                    writer.WriteUInt24(uint24Timestamp);
                    break;
                case ChunkMessageHeaderType.Continuation:
                    break;
                default:
                    throw new ArgumentException("headerType");
            }

            if (uint24Timestamp >= 0xFFFFFF)
                writer.WriteInt32(header.Timestamp);
        }

        byte[] GetMessageBytes(RtmpEvent message, Action<AmfWriter, RtmpEvent> handler)
        {
            using (var stream = new MemoryStream())
            using (var messageWriter = new AmfWriter(stream, writer.SerializationContext, objectEncoding))
            {
                handler(messageWriter, message);
                return stream.ToArray();
            }
        }
        byte[] GetMessageBytes(RtmpHeader header, RtmpEvent message)
        {
            switch (header.MessageType)
            {
                case MessageType.SetChunkSize:
                    return GetMessageBytes(message, (w, o) => w.WriteInt32(((ChunkSize)o).Size));
                case MessageType.AbortMessage:
                    return GetMessageBytes(message, (w, o) => w.WriteInt32(((Abort)o).StreamId));
                case MessageType.Acknowledgement:
                    return GetMessageBytes(message, (w, o) => w.WriteInt32(((Acknowledgement)o).BytesRead));
                case MessageType.UserControlMessage:
                    return GetMessageBytes(message, (w, o) =>
                    {
                        var m = (UserControlMessage)o;
                        w.WriteUInt16((ushort)m.EventType);
                        foreach (var v in m.Values)
                            w.WriteInt32(v);
                    });
                case MessageType.WindowAcknowledgementSize:
                    return GetMessageBytes(message, (w, o) => w.WriteInt32(((WindowAcknowledgementSize)o).Count));
                case MessageType.SetPeerBandwith:
                    return GetMessageBytes(message, (w, o) =>
                    {
                        var m = (PeerBandwidth)o;
                        w.WriteInt32(m.AcknowledgementWindowSize);
                        w.WriteByte((byte)m.LimitType);
                    });
                case MessageType.Audio:
                case MessageType.Video:
                    return GetMessageBytes(message, (w, o) => w.WriteBytes(((ByteData)o).Data));


                case MessageType.DataAmf0:
                    return GetMessageBytes(message, (w, o) => WriteData(w, o, ObjectEncoding.Amf0));
                case MessageType.SharedObjectAmf0:
                    return new byte[0]; // todo: `SharedObject`s
                case MessageType.CommandAmf0:
                    return GetMessageBytes(message, (w, o) => WriteCommandOrData(w, o, ObjectEncoding.Amf0));


                case MessageType.DataAmf3:
                    return GetMessageBytes(message, (w, o) => WriteData(w, o, ObjectEncoding.Amf3));
                case MessageType.SharedObjectAmf3:
                    return new byte[0]; // todo: `SharedObject`s
                case MessageType.CommandAmf3:
                    return GetMessageBytes(message, (w, o) =>
                    {
                        w.WriteByte(0);
                        WriteCommandOrData(w, o, ObjectEncoding.Amf3);
                    });

                case MessageType.Aggregate:
                    // todo: Aggregate messages
                    System.Diagnostics.Debugger.Break();
                    return new byte[0]; // todo: `Aggregate`
                default:
                    throw new ArgumentOutOfRangeException("Unknown RTMP message type: " + (int)header.MessageType);
            }
        }

        void WriteData(AmfWriter writer, RtmpEvent o, ObjectEncoding encoding)
        {
            var command = o as Command;
            if (command.MethodCall == null)
                WriteCommandOrData(writer, o, encoding);
            else
                writer.WriteBytes(command.Buffer);
        }

        void WriteCommandOrData(AmfWriter writer, RtmpEvent o, ObjectEncoding encoding)
        {
            var command = o as Command;
            var methodCall = command.MethodCall;
            var isInvoke = command is Invoke;

            // write the method name or result type (first section)
            var isRequest = methodCall.CallStatus == CallStatus.Request;
            if (isRequest)
                writer.WriteAmfItem(encoding, methodCall.Name);
            else
                writer.WriteAmfItem(encoding, methodCall.IsSuccess ? "_result" : "_error");

            if (isInvoke)
            {
                writer.WriteAmfItem(encoding, command.InvokeId);
                writer.WriteAmfItem(encoding, command.ConnectionParameters);
            }

            if (isRequest)
            {
                // write arguments
                foreach (var arg in methodCall.Parameters)
                    writer.WriteAmfItem(encoding, arg);
            }
            else if (isInvoke)
            {
                // write the result
                if (!methodCall.IsSuccess)
                    methodCall.Parameters = new object[] { new StatusAsObject(StatusCode.CallFailed, "error", "Call failed.") };
                writer.WriteAmfItem(encoding, methodCall.Parameters);
            }
        }
    }
}
