using RtmpSharp.IO;
using System;

namespace RtmpSharp.Messaging.Messages
{
    [Serializable]
    [SerializedName("DSK", Canonical = false)]
    [SerializedName("flex.messaging.messages.AcknowledgeMessage")]
    class AcknowledgeMessage : FlexMessage
    {
        public AcknowledgeMessage()
        {
            Timestamp = Environment.TickCount;
        }
    }
}
