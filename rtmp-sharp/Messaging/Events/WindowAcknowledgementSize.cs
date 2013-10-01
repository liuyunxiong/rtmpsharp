
namespace RtmpSharp.Messaging.Events
{
    class WindowAcknowledgementSize : RtmpEvent
    {
        // """
        // The receiving peer MUST send an Acknowledgement (Section 5.4.3) after
        // receiving the indicated number of bytes since the last Acknowledgement was
        // sent, or from the beginning of the session if no Acknowledgement has yet been
        // sent
        // """
        public int Count;

        public WindowAcknowledgementSize(int count) : base(Net.MessageType.WindowAcknowledgementSize)
        {
            Count = count;
        }
    }
}
