using System;

namespace RtmpSharp.Messaging.Messages
{
    [RtmpSharp("flex.messaging.messages.AcknowledgeMessage", "DSK")]
    class AcknowledgeMessage : FlexMessage
    {
        public AcknowledgeMessage()
            => Timestamp = Environment.TickCount;
    }
}
