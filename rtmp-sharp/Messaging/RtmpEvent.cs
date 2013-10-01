using RtmpSharp.Net;

namespace RtmpSharp.Messaging
{
    abstract class RtmpEvent
    {
        public RtmpHeader Header { get; set; }
        public int Timestamp { get; set; }
        public MessageType MessageType { get; set; }

        protected RtmpEvent(MessageType messageType)
        {
            MessageType = messageType;
        }
    }
}
