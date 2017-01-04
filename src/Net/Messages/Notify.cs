// field is never assigned to, and will always have its default value null
#pragma warning disable CS0649

namespace RtmpSharp.Net.Messages
{
    class Notify : RtmpMessage
    {
        public object Data;

        protected Notify(PacketContentType type) : base(type) { }
    }

    class NotifyAmf0 : Notify
    {
        public NotifyAmf0()
            : base(PacketContentType.DataAmf0) { }
    }

    class NotifyAmf3 : Notify
    {
        public NotifyAmf3()
            : base(PacketContentType.DataAmf3) { }
    }
}
