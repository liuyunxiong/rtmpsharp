
namespace RtmpSharp.Messaging.Events
{
    class PeerBandwith : RtmpEvent
    {
        public enum BandwithLimitType : byte
        {
            Hard = 0,
            Soft = 1,
            Dynamic = 2
        }

        public int AcknowledgementWindowSize { get; private set; }
        public BandwithLimitType LimitType { get; private set; }

        private PeerBandwith() : base(Net.MessageType.SetPeerBandwith) { }

        public PeerBandwith(int acknowledgementWindowSize, BandwithLimitType limitType) : this()
        {
            AcknowledgementWindowSize = acknowledgementWindowSize;
            LimitType = limitType;
        }

        public PeerBandwith(int acknowledgementWindowSize, byte limitType) : this()
        {
            AcknowledgementWindowSize = acknowledgementWindowSize;
            LimitType = (BandwithLimitType)limitType;
        }
    }
}
