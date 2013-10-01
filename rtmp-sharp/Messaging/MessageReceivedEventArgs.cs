using System;

namespace RtmpSharp.Messaging
{
    public class MessageReceivedEventArgs : EventArgs
    {
        public readonly object Body;
        public readonly string ClientId;
        public readonly string Subtopic;

        internal MessageReceivedEventArgs(string clientId, string subtopic, object body)
        {
            ClientId = clientId;
            Subtopic = subtopic;
            Body = body;
        }
    }
}
