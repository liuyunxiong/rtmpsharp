using RtmpSharp.IO;
using System;

namespace RtmpSharp.Messaging.Messages
{
    [Serializable]
    [SerializedName("flex.messaging.messages.RemotingMessage")]
    class RemotingMessage : FlexMessage
    {
        [SerializedName("source")]
        public string Source { get; set; }

        [SerializedName("operation")]
        public string Operation { get; set; }
    }
}
