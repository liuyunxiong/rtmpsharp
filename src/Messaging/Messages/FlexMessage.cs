using System;
using System.Collections.Generic;
using Hina.Collections;

// field is never assigned to, and will always have its default value null
#pragma warning disable CS0649

namespace RtmpSharp.Messaging.Messages
{
    class FlexMessage
    {
        IDictionary<string, object> headers;

        [RtmpSharp("clientId")]
        public string ClientId;

        [RtmpSharp("destination")]
        public string Destination;

        [RtmpSharp("messageId")]
        public string MessageId;

        [RtmpSharp("timestamp")]
        public long Timestamp;

        // ttl, in milliseconds, after `timestamp` that this message remains valid
        [RtmpSharp("timeToLive")]
        public long TimeToLive;

        [RtmpSharp("body")]
        public object Body;

        [RtmpSharp("headers")]
        public IDictionary<string, object> Headers
        {
            get => headers ?? (headers = new KeyDictionary<string, object>());
            set => headers = value;
        }

        public FlexMessage()
            => MessageId = Guid.NewGuid().ToString("D");
    }

    static class FlexMessageHeaders
    {
        // messages pushed from the server may arrive in a batch, with messages in the batch potentially targeted to
        // different consumer instances.
        //
        // each message will contain this header identifying the consumer instance that will receive the message.
        public const string DestinationClientId = "DSDstClientId";

        // messages are tagged with the endpoint id for the channel they are sent over. channels set this value
        // automatically when they send a message.
        public const string Endpoint = "DSEndpoint";

        // messages that need to set remote credentials for a destination carry the base64 encoded credentials in this
        // header.
        public const string RemoteCredentials = "DSRemoteCredentials";

        // messages sent with a defined request timeout use this header.
        //
        // the request timeout value is set on outbound messages by services or channels and the value
        // controls how long the corresponding MessageResponder will wait for an acknowledgement,
        // result or fault response for the message before timing out the request.
        public const string RequestTimeout = "DSRequestTimeout";

        // this header is used to transport the global flex client id in outbound messages, once it has been assigned
        // by the server.
        public const string FlexClientId = "DSId";
    }
}
