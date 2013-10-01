using RtmpSharp.IO;
using RtmpSharp.Messaging;
using RtmpSharp.Messaging.Events;
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization;

namespace RtmpSharp.Net
{
    // Shared objects not implemented (yet)
    // AMF3 data messages aren't handled (what's in them?)
    // Multimedia packets aren't handled
    class RtmpPacketReader
    {
        public bool Continue { get; set; }

        public event EventHandler<EventReceivedEventArgs> EventReceived;
        public event EventHandler Disconnected;

        AmfReader reader;

        // Defined by the spec
        const int DefaultChunkSize = 128;

        Dictionary<int, RtmpHeader> rtmpHeaders;
        Dictionary<int, RtmpPacket> rtmpPackets;
        int readChunkSize = DefaultChunkSize;

        public RtmpPacketReader(AmfReader reader)
        {
            this.reader = reader;

            rtmpHeaders = new Dictionary<int, RtmpHeader>();
            rtmpPackets = new Dictionary<int, RtmpPacket>();

            Continue = true;
        }

        void OnEventReceived(EventReceivedEventArgs e)
        {
            if (EventReceived != null)
                EventReceived(this, e);
        }

        void OnDisconnected(EventArgs e)
        {
            if (Disconnected != null)
                Disconnected(this, e);
        }

        public void ReadLoop()
        {
            try
            {
                while (Continue)
                {
                    var header = ReadHeader();
                    rtmpHeaders[header.StreamId] = header;

                    RtmpPacket packet;
                    if (!rtmpPackets.TryGetValue(header.StreamId, out packet) || packet == null)
                    {
                        packet = new RtmpPacket(header);
                        rtmpPackets[header.StreamId] = packet;
                    }

                    var remainingMessageLength = packet.Length + (header.Timestamp >= 0xFFFFFF ? 4 : 0) - packet.CurrentLength;
                    var bytesToRead = Math.Min(remainingMessageLength, readChunkSize);
                    var bytes = reader.ReadBytes(bytesToRead);
                    packet.AddBytes(bytes);

                    if (packet.IsComplete)
                    {
                        rtmpPackets.Remove(header.StreamId);

                        var @event = ParsePacket(packet);
                        OnEventReceived(new EventReceivedEventArgs(@event));

                        // process some kinds of packets
                        var chunkSizeMessage = @event as ChunkSize;
                        if (chunkSizeMessage != null)
                            readChunkSize = chunkSizeMessage.Size;

                        var abortMessage = @event as Abort;
                        if (abortMessage != null)
                            rtmpPackets.Remove(abortMessage.StreamId);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.Print("Exception: {0} at {1}", ex.ToString(), ex.StackTrace);
                if (ex.InnerException != null)
                {
                    var inner = ex.InnerException;
                    System.Diagnostics.Debug.Print("InnerException: {0} at {1}", inner.ToString(), inner.StackTrace);
                }
                OnDisconnected(new EventArgs());
            }
        }

        static int GetChunkStreamId(byte chunkBasicHeaderByte, AmfReader reader)
        {
            var chunkStreamId = chunkBasicHeaderByte & 0x3F;

            // 2 bytes
            if (chunkStreamId == 0)
                return reader.ReadByte() + 64;

            // 3 bytes
            if (chunkStreamId == 1)
                return reader.ReadByte() + reader.ReadByte() * 256 + 64;

            return chunkStreamId;
        }

        RtmpHeader ReadHeader()
        {
            // first byte of the chunk basic header
            var chunkBasicHeaderByte = reader.ReadByte();
            var chunkStreamId = GetChunkStreamId(chunkBasicHeaderByte, reader);
            var chunkMessageHeaderType = (ChunkMessageHeaderType)(chunkBasicHeaderByte >> 6);

            var header = new RtmpHeader()
            {
                StreamId = chunkStreamId,
                IsTimerRelative = chunkMessageHeaderType != ChunkMessageHeaderType.New
            };

            RtmpHeader previousHeader;
            // don't need to clone if new header, as it contains all info
            if (!rtmpHeaders.TryGetValue(chunkStreamId, out previousHeader) && chunkMessageHeaderType != ChunkMessageHeaderType.New)
                previousHeader = header.Clone();

            switch (chunkMessageHeaderType)
            {
                // 11 bytes
                case ChunkMessageHeaderType.New:
                    header.Timestamp = reader.ReadUInt24();
                    header.PacketLength = reader.ReadUInt24();
                    header.MessageType = (MessageType)reader.ReadByte();
                    header.MessageStreamId = reader.ReadReverseInt();
                    break;

                // 7 bytes
                case ChunkMessageHeaderType.SameSource:
                    header.Timestamp = reader.ReadUInt24();
                    header.PacketLength = reader.ReadUInt24();
                    header.MessageType = (MessageType)reader.ReadByte();
                    header.MessageStreamId = previousHeader.MessageStreamId;
                    break;

                // 3 bytes
                case ChunkMessageHeaderType.TimestampAdjustment:
                    header.Timestamp = reader.ReadUInt24();
                    header.PacketLength = previousHeader.PacketLength;
                    header.MessageType = previousHeader.MessageType;
                    header.MessageStreamId = previousHeader.MessageStreamId;
                    break;

                // 0 bytes
                case ChunkMessageHeaderType.Continuation:
                    header.Timestamp = previousHeader.Timestamp;
                    header.PacketLength = previousHeader.PacketLength;
                    header.MessageType = previousHeader.MessageType;
                    header.MessageStreamId = previousHeader.MessageStreamId;
                    header.IsTimerRelative = previousHeader.IsTimerRelative;
                    break;
                default:
                    throw new SerializationException("Unexpected header size.");
            }

            // extended timestamp
            if (header.Timestamp == 0xFFFFFF)
                header.Timestamp = reader.ReadInt32();

            return header;
        }

        RtmpEvent ParsePacket(RtmpPacket packet, Func<AmfReader, RtmpEvent> handler)
        {
            var memoryStream = new MemoryStream(packet.Buffer, false);
            var packetReader = new AmfReader(memoryStream, reader.SerializationContext, reader.DeserializeToObjects, reader.DeserializeToDynamicWhenTypeNotFound);

            var header = packet.Header;
            var message = handler(packetReader);
            message.Header = header;
            message.Timestamp = header.Timestamp;
            return message;
        }
        RtmpEvent ParsePacket(RtmpPacket packet)
        {
            switch (packet.Header.MessageType)
            {
                case MessageType.SetChunkSize:
                    return ParsePacket(packet, r => new ChunkSize(r.ReadInt32()));
                case MessageType.AbortMessage:
                    return ParsePacket(packet, r => new Abort(r.ReadInt32()));
                case MessageType.Acknowledgement:
                    return ParsePacket(packet, r => new Acknowledgement(r.ReadInt32()));
                case MessageType.UserControlMessage:
                    return ParsePacket(packet, r =>
                    {
                        var eventType = r.ReadUInt16();
                        var values = new List<int>();
                        while (r.Length - r.Position >= 4)
                            values.Add(r.ReadInt32());
                        return new UserControlMessage((UserControlMessageType)eventType, values.ToArray());
                    });
                case MessageType.WindowAcknowledgementSize:
                    return ParsePacket(packet, r => new WindowAcknowledgementSize(r.ReadInt32()));
                case MessageType.SetPeerBandwith:
                    return ParsePacket(packet, r => new PeerBandwith(r.ReadInt32(), r.ReadByte()));
                case MessageType.Audio:
                    return ParsePacket(packet, r => new AudioData(packet.Buffer));
                case MessageType.Video:
                    return ParsePacket(packet, r => new VideoData(packet.Buffer));


                case MessageType.DataAmf0:
                    return ParsePacket(packet, r => ReadCommandOrData(r, new NotifyAmf0()));
                case MessageType.SharedObjectAmf0:
                    break;
                case MessageType.CommandAmf0:
                    return ParsePacket(packet, r => ReadCommandOrData(r, new InvokeAmf0()));


                case MessageType.DataAmf3:
                    return ParsePacket(packet, r => ReadCommandOrData(r, new NotifyAmf3()));
                case MessageType.SharedObjectAmf3:
                    break;
                case MessageType.CommandAmf3:
                    return ParsePacket(packet, r =>
                    {
                        // encoding? always seems to be zero
                        var unk1 = r.ReadByte();
                        return ReadCommandOrData(r, new InvokeAmf3());
                    });


                // Only seems to be used in audio and video streams, so we should be OK until we decide to implement aggregate packets.
                case MessageType.Aggregate:
                    throw new ArgumentOutOfRangeException("Unknown RTMP message type: " + (int)packet.Header.MessageType);
                default:
#if DEBUG
                    // Find out how to handle this message type.
                    System.Diagnostics.Debugger.Break();
#endif
                    break;
            }

            // Skip messages we don't understand
            return null;
        }

        static RtmpEvent ReadCommandOrData(AmfReader r, Command command)
        {
            var methodName = (string)r.ReadAmf0Item();

            command.InvokeId = Convert.ToInt32(r.ReadAmf0Item());
            command.ConnectionParameters = r.ReadAmf0Item();

            var parameters = new List<object>();
            while (r.DataAvailable)
                parameters.Add(r.ReadAmf0Item());

            command.MethodCall = new Method(methodName, parameters.ToArray());
            return command;
        }
    }
}
