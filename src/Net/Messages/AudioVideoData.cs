namespace RtmpSharp.Net.Messages
{
    abstract class ByteData : RtmpMessage
    {
        public byte[] Data;

        protected ByteData(byte[] data, PacketContentType type) : base(type)
            => Data = data;
    }

    class AudioData : ByteData
    {
        public AudioData(byte[] data) : base(data, PacketContentType.Audio) { }
    }

    class VideoData : ByteData
    {
        public VideoData(byte[] data) : base(data, PacketContentType.Video) { }
    }
}
