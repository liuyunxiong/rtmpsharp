using RtmpSharp.Net;

namespace RtmpSharp.Messaging.Events
{
    enum CallStatus
    {
        Request,
        Result,
    }

    class Method
    {
        public CallStatus CallStatus { get; internal set; }
        public string Name { get; internal set; }
        public bool IsSuccess { get; internal set; }
        public object[] Parameters { get; internal set; }

        internal Method(string methodName, object[] parameters)
        {
            Name = methodName;
            Parameters = parameters;
            CallStatus = CallStatus.Request;
        }
    }

    class Command : RtmpEvent
    {
        public Method MethodCall { get; internal set; }
        public byte[] Buffer { get; internal set; }
        public int InvokeId { get; internal set; }
        public object ConnectionParameters { get; internal set; }

        public Command(MessageType messageType) : base(messageType) { }
    }

    abstract class Invoke : Command
    {
        protected Invoke(MessageType messageType) : base(messageType) { }
    }

    abstract class Notify : Command
    {
        protected Notify(MessageType messageType) : base(messageType) { }
    }

    class InvokeAmf3 : Invoke
    {
        public InvokeAmf3() : base(Net.MessageType.CommandAmf3) { }
    }

    class NotifyAmf3 : Notify
    {
        public NotifyAmf3() : base(Net.MessageType.DataAmf3) { }
    }

    class InvokeAmf0 : Invoke
    {
        public InvokeAmf0() : base(Net.MessageType.CommandAmf0) { }
    }

    class NotifyAmf0 : Notify
    {
        public NotifyAmf0() : base(Net.MessageType.DataAmf0) { }
    }
}
