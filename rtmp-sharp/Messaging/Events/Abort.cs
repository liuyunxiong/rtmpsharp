
namespace RtmpSharp.Messaging.Events
{
    class Abort : RtmpEvent
    {
        public int StreamId { get; private set; }

        public Abort(int streamId) : base(Net.MessageType.AbortMessage)
        {
            StreamId = streamId;
        }
    }
}
