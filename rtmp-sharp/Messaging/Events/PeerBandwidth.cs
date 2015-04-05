
namespace RtmpSharp.Messaging.Events
{
    class PeerBandwidth : RtmpEvent
    {
        public enum BandwithLimitType : byte
        {
            Hard = 0,
            Soft = 1,
            Dynamic = 2
        }

        public int AcknowledgementWindowSize { get; private set; }
        public BandwithLimitType LimitType { get; private set; }

        private PeerBandwidth() : base(Net.MessageType.SetPeerBandwith) { }

        public PeerBandwidth(int acknowledgementWindowSize, BandwithLimitType limitType) : this()
        {
            AcknowledgementWindowSize = acknowledgementWindowSize;
            LimitType = limitType;
        }

        public PeerBandwidth(int acknowledgementWindowSize, byte limitType) : this()
        {
            AcknowledgementWindowSize = acknowledgementWindowSize;
            LimitType = (BandwithLimitType)limitType;
        }
    }
}
