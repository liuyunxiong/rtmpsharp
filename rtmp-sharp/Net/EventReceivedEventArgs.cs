using RtmpSharp.Messaging;
using System;

namespace RtmpSharp.Net
{
    class EventReceivedEventArgs : EventArgs
    {
        public RtmpEvent Event { get; set; }

        public EventReceivedEventArgs(RtmpEvent @event)
        {
            this.Event = @event;
        }
    }
}
