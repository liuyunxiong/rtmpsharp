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


        internal InvocationException(object source, string faultCode, string faultString, string faultDetail, object rootCause, object extendedData)
        {
            FaultCode       = faultCode;
            FaultString     = faultString;
            FaultDetail     = faultDetail;
            RootCause       = rootCause;
            ExtendedData    = extendedData;

            SourceException = source;
        }

        public InvocationException()
        {
        }
    }
}
