using RtmpSharp.IO;
using System;

namespace RtmpSharp.Messaging.Messages
{
    enum CommandOperation : int
    {
        Subscribe = 0,
        Unsubscribe = 1,
        Poll = 2, // poll for undelivered messages
        DataUpdateAttributes = 3,
        ClientSync = 4, // sent by remote to sync missed or cached messages to a client as a result of a client issued poll command
        ClientPing = 5, // connectivity test
        DataUpdate = 7,
        ClusterRequest = 7, // request a list of failover endpoint URIs for the remote destination based on cluster membership
        Login = 8,
        Logout = 9,
        InvalidateSubscription = 10, // indicates that client subscription has been invalidated (eg timed out)
        ChannelDisconnected = 12, // indicates that a channel has disconnected
        Unknown = 10000
    }

    [Serializable]
    [SerializedName("DSC", Canonical = false)]
    [SerializedName("flex.messaging.messages.CommandMessage")]
    class CommandMessage : AsyncMessage
    {
        [SerializedName("messageRefType")]
        public string MessageRefType { get; set; }

        [SerializedName("operation")]
        public CommandOperation Operation { get; set; }

        public CommandMessage()
        {
        }
    }
}
