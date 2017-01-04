using System;
using RtmpSharp.Messaging.Messages;

namespace RtmpSharp.Messaging
{
    public class InvocationException : Exception
    {
        public string FaultCode;
        public string FaultString;
        public string FaultDetail;
        public object RootCause;
        public object ExtendedData;
        public object SourceException;

        public override string Message    => FaultString;
        public override string StackTrace => FaultDetail;

        internal InvocationException(ErrorMessage message)
        {
            SourceException = message;

            FaultCode    = message.FaultCode;
            FaultString  = message.FaultString;
            FaultDetail  = message.FaultDetail;
            RootCause    = message.RootCause;
            ExtendedData = message.ExtendedData;
        }

        public InvocationException()
        {
        }
    }
}
