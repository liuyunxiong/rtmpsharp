namespace RtmpSharp.Net.Messages
{
    class PeerBandwidth : RtmpMessage
    {
        public int AckWindowSize;
        public BandwithLimitType LimitType;

        public PeerBandwidth(int windowSize, BandwithLimitType type) : base(PacketContentType.SetPeerBandwith)
        {
            AckWindowSize = windowSize;
            LimitType     = type;
        }

        public PeerBandwidth(int acknowledgementWindowSize, byte type) : base(PacketContentType.SetPeerBandwith)
        {
            AckWindowSize = acknowledgementWindowSize;
            LimitType     = (BandwithLimitType)type;
        }

        public enum BandwithLimitType : byte
        {
            Hard    = 0,
            Soft    = 1,
            Dynamic = 2
        }
    }
}
