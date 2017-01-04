// field is never assigned to, and will always have its default value null
#pragma warning disable CS0649

namespace RtmpSharp.Messaging.Messages
{
    [RtmpSharp("flex.messaging.messages.ErrorMessage")]
    class ErrorMessage : FlexMessage
    {
        [RtmpSharp("faultCode")]
        public string FaultCode;

        [RtmpSharp("faultString")]
        public string FaultString;

        [RtmpSharp("faultDetail")]
        public string FaultDetail;

        [RtmpSharp("rootCause")]
        public object RootCause;

        [RtmpSharp("extendedData")]
        public object ExtendedData;
    }
}
