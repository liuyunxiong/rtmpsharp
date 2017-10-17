using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Hina;
using Hina.Collections;
using Hina.Net;
using Hina.Threading;
using Konseki;
using RtmpSharp.Messaging;
using RtmpSharp.Messaging.Messages;
using RtmpSharp.Net.Messages;

namespace RtmpSharp.Net
{
    public partial class RtmpClient
    {
        public event EventHandler<MessageReceivedEventArgs>    MessageReceived;
        public event EventHandler<ClientDisconnectedException> Disconnected;
        public event EventHandler<Exception>                   CallbackException;

        // the cancellation source (and token) that this client internally uses to signal disconnection
        readonly CancellationToken token;
        readonly CancellationTokenSource source;

        // the serialization context for this rtmp client
        readonly SerializationContext context;

        // the callback manager that handles completing invocation requests
        readonly TaskCallbackManager<uint, object> callbacks;

        // fn(message: RtmpMessage, chunk_stream_id: int) -> None
        //     queues a message to be written. this is assigned post-construction by `connectasync`.
        Action<RtmpMessage, int> queue;

        // the client id that was assigned to us by the remote peer. this is assigned post-construction by
        // `connectasync`, and may be null if no explicit client id was provided.
        string clientId;

        // counter for monotonically increasing invoke ids
        int invokeId;

        // true if this connection is no longer connected
        bool disconnected;

        // a tuple describing the cause of the disconnection. either value may be null.
        (string message, Exception inner) cause;


        RtmpClient(SerializationContext context)
        {
            this.context   = context;
            this.callbacks = new TaskCallbackManager<uint, object>();
            this.source    = new CancellationTokenSource();
            this.token     = source.Token;
        }


        #region internal callbacks

        // `internalreceivesubscriptionvalue` will never throw an exception
        void InternalReceiveSubscriptionValue(string clientId, string subtopic, object body)
        {
            WrapCallback(() => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(clientId, subtopic, body)));
        }

        // called internally by the readers and writers when an error that would invalidate this connection occurs.
        // `inner` may be null.
        void InternalCloseConnection(string reason, Exception inner)
        {
            Volatile.Write(ref cause.message, reason);
            Volatile.Write(ref cause.inner,   inner);
            Volatile.Write(ref disconnected,  true);

            source.Cancel();
            callbacks.SetExceptionForAll(DisconnectedException());

            WrapCallback(() => Disconnected?.Invoke(this, DisconnectedException()));
        }

        // this method will never throw an exception unless that exception will be fatal to this connection, and thus
        // the connection would be forced to close.
        void InternalReceiveEvent(RtmpMessage message)
        {
            switch (message)
            {
                case UserControlMessage u when u.EventType == UserControlMessage.Type.PingRequest:
                    queue(new UserControlMessage(UserControlMessage.Type.PingResponse, u.Values), 2);
                    break;

                case Invoke i:
                    var param = i.Arguments?.FirstOrDefault();

                    switch (i.MethodName)
                    {
                        case "_result":
                            // unwrap the flex wrapper object if it is present
                            var a = param as AcknowledgeMessage;
                            callbacks.SetResult(i.InvokeId, a?.Body ?? param);
                            break;

                        case "_error":
                            // unwrap the flex wrapper object if it is present
                            var b = param as ErrorMessage;
                            callbacks.SetException(i.InvokeId, b != null ? new InvocationException(b) : new InvocationException());
                            break;

                        case "receive":
                            if (param is AsyncMessage c)
                                InternalReceiveSubscriptionValue(c.ClientId, c.Headers.GetDefault(AsyncMessageHeaders.Subtopic) as string, c.Body);
                            break;

                        case "onstatus":
                            Kon.Trace("received status");
                            break;

                        // [2016-12-26] workaround roslyn compiler bug that would cause the following default cause to
                        // cause a nullreferenceexception on the owning switch statement.
                        //     default:
                        //         Kon.DebugRun(() =>
                        //         {
                        //             Kon.Trace("unknown rtmp invoke method requested", new { method = i.MethodName, args = i.Arguments });
                        //             Debugger.Break();
                        //         });
                        //
                        //         break;

                        default:
                            break;
                    }

                    break;
            }
        }

        #endregion


        #region internal helper methods

        uint NextInvokeId() => (uint)Interlocked.Increment(ref invokeId);
        ClientDisconnectedException DisconnectedException() => new ClientDisconnectedException(cause.message, cause.inner);

        // calls a remote endpoint, sent along the specified chunk stream id, on message stream id #0
        Task<object> InternalCallAsync(Invoke request, int chunkStreamId)
        {
            if (disconnected) throw DisconnectedException();

            var task = callbacks.Create(request.InvokeId);

            queue(request, chunkStreamId);
            return task;
        }

        void WrapCallback(Action action)
        {
            try
            {
                try { action(); }
                catch (Exception e) { CallbackException?.Invoke(this, e); }
            }
            catch (Exception e)
            {
                Kon.DebugRun(() =>
                {
                    Kon.DebugException("unhandled exception in callback", e);
                    Debugger.Break();
                });
            }
        }

        #endregion


        #region (static) connectasync()

        public class Options
        {
            public string               Url;
            public int                  ChunkLength = 4192;
            public SerializationContext Context;

            // the below fields are optional, and may be null
            public string AppName;
            public string PageUrl;
            public string SwfUrl;

            public RemoteCertificateValidationCallback Validate;
        }

        public static async Task<RtmpClient> ConnectAsync(Options options, params object[] arguments)
        {
            Check.NotNull(options.Url, options.Context);


            var url         = options.Url;
            var chunkLength = options.ChunkLength;
            var context     = options.Context;
            var validate    = options.Validate ?? ((sender, certificate, chain, errors) => true);

            var appName     = options.AppName;
            var pageUrl     = options.PageUrl;
            var swfUrl      = options.SwfUrl;


            var uri    = new Uri(url);
            var tcp    = await TcpClientEx.ConnectAsync(uri.Host, uri.Port);
            var stream = await GetStreamAsync(uri, tcp.GetStream(), validate);

            await Handshake.GoAsync(stream);

            var client = new RtmpClient(context);
            var reader = new Reader(client, stream, context, client.token);
            var writer = new Writer(client, stream, context, client.token);

            reader.RunAsync().Forget();
            writer.RunAsync(chunkLength).Forget();

            client.queue    = (message, chunkStreamId) => writer.QueueWrite(message, chunkStreamId);
            client.clientId = await RtmpConnectAsync(
                client:  client,
                appName: appName,
                pageUrl: pageUrl,
                swfUrl:  swfUrl,
                tcUrl:   uri.ToString(),
                arguments: arguments);

            return client;
        }

        static async Task<Stream> GetStreamAsync(Uri uri, Stream stream, RemoteCertificateValidationCallback validate)
        {
            CheckDebug.NotNull(uri, stream, validate);

            switch (uri.Scheme)
            {
                case "rtmp":
                    return stream;

                case "rtmps":
                    Check.NotNull(validate);

                    var ssl = new SslStream(stream, false, validate);
                    await ssl.AuthenticateAsClientAsync(uri.Host);

                    return ssl;

                default:
                    throw new ArgumentException($"scheme \"{uri.Scheme}\" must be one of rtmp:// or rtmps://");
            }
        }

        // attempts to perform an rtmp connect, and returns the client id assigned to us (if any - this may be null)
        static async Task<string> RtmpConnectAsync(RtmpClient client, string appName, string pageUrl, string swfUrl, string tcUrl, params object[] arguments)
        {
            var request = new InvokeAmf0
            {
                InvokeId   = client.NextInvokeId(),
                MethodName = "connect",
                Arguments  = arguments,
                Headers    = new AsObject()
                {
                    { "app",            appName          },
                    { "audioCodecs",    3575             },
                    { "capabilities",   239              },
                    { "flashVer",       "WIN 21,0,0,174" },
                    { "fpad",           false            },
                    { "objectEncoding", (double)3        }, // currently hard-coded to amf3
                    { "pageUrl",        pageUrl          },
                    { "swfUrl",         swfUrl           },
                    { "tcUrl",          tcUrl            },
                    { "videoCodecs",    252              },
                    { "videoFunction",  1                },
                },
            };

            var response = await client.InternalCallAsync(request, chunkStreamId: 3) as IDictionary<string, object>;

            return response != null && (response.TryGetValue("clientId", out var clientId) || response.TryGetValue("id", out clientId))
                ? clientId as string
                : null;
        }

        #endregion


        #region rtmpclient methods

        // some servers will fail if `destination` is null (but not if it's an empty string)
        const string NoDestination = "";

        public async Task<T> InvokeAsync<T>(string method, params object[] arguments)
            => NanoTypeConverter.ConvertTo<T>(
                await InternalCallAsync(new InvokeAmf0() { MethodName = method, Arguments = arguments, InvokeId = NextInvokeId() }, 3));

        public async Task<T> InvokeAsync<T>(string endpoint, string destination, string method, params object[] arguments)
        {
            // this is a flex-style invoke, which *requires* amf3 encoding. fortunately, we always default to amf3
            // decoding and don't have a way to specify amf0 encoding in this iteration of rtmpclient, so no check is
            // needed.

             var request = new InvokeAmf3()
            {
                InvokeId   = NextInvokeId(),
                MethodName = null,
                Arguments  = new[]
                {
                    new RemotingMessage
                    {
                        ClientId    = Guid.NewGuid().ToString("D"),
                        Destination = destination,
                        Operation   = method,
                        Body        = arguments,
                        Headers     = new StaticDictionary<string, object>()
                        {
                            { FlexMessageHeaders.Endpoint,     endpoint },
                            { FlexMessageHeaders.FlexClientId, clientId ?? "nil" }
                        }
                    }
                }
            };

            return NanoTypeConverter.ConvertTo<T>(
                await InternalCallAsync(request, chunkStreamId: 3));
        }

        public async Task<bool> SubscribeAsync(string endpoint, string destination, string subtopic, string clientId)
        {
            Check.NotNull(endpoint, destination, subtopic, clientId);

            var message = new CommandMessage
            {
                ClientId      = clientId,
                CorrelationId = null,
                Operation     = CommandMessage.Operations.Subscribe,
                Destination   = destination,
                Headers       = new StaticDictionary<string, object>()
                {
                    { FlexMessageHeaders.Endpoint,     endpoint },
                    { FlexMessageHeaders.FlexClientId, clientId },
                    { AsyncMessageHeaders.Subtopic,    subtopic }
                }
            };

            return await InvokeAsync<string>(null, message) == "success";
        }

        public async Task<bool> UnsubscribeAsync(string endpoint, string destination, string subtopic, string clientId)
        {
            Check.NotNull(endpoint, destination, subtopic, clientId);

            var message = new CommandMessage
            {
                ClientId      = clientId,
                CorrelationId = null,
                Operation     = CommandMessage.Operations.Unsubscribe,
                Destination   = destination,
                Headers       = new KeyDictionary<string, object>()
                {
                    { FlexMessageHeaders.Endpoint,     endpoint },
                    { FlexMessageHeaders.FlexClientId, clientId },
                    { AsyncMessageHeaders.Subtopic,    subtopic }
                }
            };

            return await InvokeAsync<string>(null, message) == "success";
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            Check.NotNull(username, password);

            var credentials = $"{username}:{password}";
            var message     = new CommandMessage
            {
                ClientId    = clientId,
                Destination = NoDestination,
                Operation   = CommandMessage.Operations.Login,
                Body        = Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)),
            };

            return await InvokeAsync<string>(null, message) == "success";
        }

        public Task LogoutAsync()
        {
            var message = new CommandMessage
            {
                ClientId    = clientId,
                Destination = NoDestination,
                Operation   = CommandMessage.Operations.Logout
            };

            return InvokeAsync<object>(null, message);
        }

        public Task PingAsync()
        {
            var message = new CommandMessage
            {
                ClientId    = clientId,
                Destination = NoDestination,
                Operation   = CommandMessage.Operations.ClientPing
            };

            return InvokeAsync<object>(null, message);
        }

        #endregion


        public Task CloseAsync(bool forced = false)
        {
            // currently we don't have a notion of gracefully closing a connection. all closes are hard force closes,
            // but we leave the possibility for properly implementing graceful closures in the future

            InternalCloseConnection("close-requested-by-user", null);

            return Task.CompletedTask;
        }
    }
}
