namespace RtmpSharp.Net.Messages
{
    class Invoke : RtmpMessage
    {
        public string   MethodName;
        public object[] Arguments;
        public uint     InvokeId;
        public object   Headers;

        public Invoke(PacketContentType type) : base(type) { }
    }

    class InvokeAmf0 : Invoke
    {
        public InvokeAmf0()
            : base(PacketContentType.CommandAmf0) { }
    }

    class InvokeAmf3 : Invoke
    {
        public InvokeAmf3()
            : base(PacketContentType.CommandAmf3) { }
    }
}
