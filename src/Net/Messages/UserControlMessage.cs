namespace RtmpSharp.Net.Messages
{
    class UserControlMessage : RtmpMessage
    {
        public Type   EventType;
        public uint[] Values;

        public UserControlMessage(Type type, uint[] values) : base(PacketContentType.UserControlMessage)
        {
            EventType = type;
            Values    = values;
        }

        public enum Type : ushort
        {
            StreamBegin      = 0,
            StreamEof        = 1,
            StreamDry        = 2,
            SetBufferLength  = 3,
            StreamIsRecorded = 4,
            PingRequest      = 6,
            PingResponse     = 7
        }
    }
}
