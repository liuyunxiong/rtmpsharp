using System;

namespace RtmpSharp.Net
{
    public class ClientDisconnectedException : Exception
    {
        public ClientDisconnectedException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
