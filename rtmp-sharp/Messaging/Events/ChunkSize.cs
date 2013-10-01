using RtmpSharp.Net;

namespace RtmpSharp.Messaging.Events
{
    class ChunkSize : RtmpEvent
    {
        public int Size { get; private set; }

        public ChunkSize(int size) : base(MessageType.SetChunkSize)
        {
            if (size > 0xFFFFFF)
                size = 0xFFFFFF;
            Size = size;
        }
    }
}
