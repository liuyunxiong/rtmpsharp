
namespace RtmpSharp.Messaging.Events
{
    // user control message
    class UserControlMessage : RtmpEvent
    {
        public UserControlMessageType EventType { get; private set; }
        public int[] Values { get; private set; }

        public UserControlMessage(UserControlMessageType eventType, int[] values) : base(Net.MessageType.UserControlMessage)
        {
            EventType = eventType;
            Values = values;
        }
    }

    enum UserControlMessageType : ushort
    {
        StreamBegin = 0,
        StreamEof = 1,
        StreamDry = 2,
        SetBufferLength = 3,
        StreamIsRecorded = 4,
        PingRequest = 6,
        PingResponse = 7
    }
}
