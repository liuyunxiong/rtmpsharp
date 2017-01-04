// field is never assigned to, and will always have its default value null
#pragma warning disable CS0649

namespace RtmpSharp.Messaging.Messages
{
    [RtmpSharp("flex.messaging.messages.RemotingMessage")]
    class RemotingMessage : FlexMessage
    {
        [RtmpSharp("source")]
        public string Source;

        [RtmpSharp("operation")]
        public string Operation;
    }
}
