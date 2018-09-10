namespace RtmpSharp.Net.Messages
{
    class Abort : RtmpMessage
    {
        public int ChunkStreamId;

        public Abort(int chunkStreamId) : base(PacketContentType.AbortMessage)
            => ChunkStreamId = chunkStreamId;
    }
}
