namespace RtmpSharp.Messaging.Messages
{
    [RtmpSharp("flex.messaging.messages.AsyncMessage", "DSA")]
    class AsyncMessage : FlexMessage
    {
        [RtmpSharp("correlationId")]
        public string CorrelationId;
    }

    static class AsyncMessageHeaders
    {
        public const string Subtopic = "DSSubtopic";
    }
}
