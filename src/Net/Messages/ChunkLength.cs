namespace RtmpSharp.Net.Messages
{
    class ChunkLength : RtmpMessage
    {
        public int Length;

        public ChunkLength(int length) : base(PacketContentType.SetChunkSize)
        {
            Length = length > 0xFFFFFF ? 0xFFFFFF : length;
        }
    }
}
