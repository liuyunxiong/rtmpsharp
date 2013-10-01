using RtmpSharp.Messaging.Messages;
using System;

namespace RtmpSharp.Messaging
{
    public class InvocationException : Exception
    {
        public string FaultCode { get; set; }
        public string FaultString { get; set; }
        public string FaultDetail { get; set; }
        public object RootCause { get; set; }
        public object ExtendedData { get; set; }
        public object SourceException { get; set; }

        internal InvocationException(ErrorMessage errorMessage)
        {
            SourceException = errorMessage;

            FaultCode = errorMessage.FaultCode;
            FaultString = errorMessage.FaultString;
            FaultDetail = errorMessage.FaultDetail;
            RootCause = errorMessage.RootCause;
            ExtendedData = errorMessage.ExtendedData;
        }

        public InvocationException()
        {
        }

        public override string Message { get { return FaultString; } }
        public override string StackTrace { get { return FaultDetail; } }
    }
}
