
namespace RtmpSharp.Messaging.Events
{
    class Abort : RtmpEvent
    {
        public int StreamId { get; }

        public Abort(int streamId) : base(Net.MessageType.AbortMessage)
        {
            StreamId = streamId;
        }
    }
}
