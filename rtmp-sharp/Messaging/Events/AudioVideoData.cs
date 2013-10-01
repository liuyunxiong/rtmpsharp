using RtmpSharp.Net;

namespace RtmpSharp.Messaging.Events
{
    abstract class ByteData : RtmpEvent
    {
        public byte[] Data { get; private set; }

        protected ByteData(byte[] data, MessageType messageType) : base(messageType)
        {
            Data = data;
        }
    }

    class AudioData : ByteData
    {
        public AudioData(byte[] data) : base(data, Net.MessageType.Audio)
        {
        }
    }

    class VideoData : ByteData
    {
        public VideoData(byte[] data) : base(data, Net.MessageType.Video)
        {
        }
    }
}
