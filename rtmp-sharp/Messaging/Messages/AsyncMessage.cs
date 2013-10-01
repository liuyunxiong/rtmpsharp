using RtmpSharp.IO;
using System;

namespace RtmpSharp.Messaging.Messages
{
    [Serializable]
    [SerializedName("DSA", Canonical = false)]
    [SerializedName("flex.messaging.messages.AsyncMessage")]
    class AsyncMessage : FlexMessage
    {
        [SerializedName("correlationId")]
        public string CorrelationId { get; set; }
    }

    static class AsyncMessageHeaders
    {
        public const string Subtopic = "DSSubtopic";
    }
}
