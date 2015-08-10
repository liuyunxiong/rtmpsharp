using Complete;
using Complete.Threading;
using RtmpSharp.IO;
using RtmpSharp.Messaging;
using RtmpSharp.Messaging.Events;
using RtmpSharp.Messaging.Messages;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RtmpSharp.Net
{
    public class RtmpClient
    {
        public event EventHandler Disconnected;
        public event EventHandler<MessageReceivedEventArgs> MessageReceived;
        public event EventHandler<Exception> CallbackException;

        public bool IsDisconnected => !hasConnected || disconnectsFired != 0;

        public string ClientId;

        public bool NoDelay = true;
        public bool ExclusiveAddressUse;
        public int ReceiveTimeout;
        public int SendTimeout;
        public IPEndPoint LocalEndPoint;

        // by default, accept all certificates
        readonly Uri uri;
        readonly ObjectEncoding objectEncoding;
        readonly TaskCallbackManager<int, object> callbackManager;
        readonly SerializationContext context;
        readonly RemoteCertificateValidationCallback validator = (sender, certificate, chain, errors) => true;
        RtmpPacketWriter writer;
        RtmpPacketReader reader;
        Thread writerThread;
        Thread readerThread;

        int invokeId;
        bool hasConnected;

        volatile int disconnectsFired;

        public RtmpClient(Uri uri, SerializationContext context)
        {
            if (uri == null) throw new ArgumentNullException(nameof(uri));
            if (context == null) throw new ArgumentNullException(nameof(context));

            var scheme = uri.Scheme.ToLowerInvariant();
            if (scheme != "rtmp" && scheme != "rtmps")
                throw new ArgumentException($"the scheme {scheme} is not supported. only rtmp:// and rtmps:// schemes are supported");

            this.uri = uri;
            this.context = context;
            this.callbackManager = new TaskCallbackManager<int, object>();
        }

        public RtmpClient(Uri uri, SerializationContext context, ObjectEncoding objectEncoding) : this(uri, context)
        {
            this.objectEncoding = objectEncoding;
        }

        public RtmpClient(Uri uri, ObjectEncoding objectEncoding, SerializationContext serializationContext, RemoteCertificateValidationCallback validator)
            : this(uri, serializationContext, objectEncoding)
        {
            if (validator == null)
                throw new ArgumentNullException(nameof(validator));

            this.validator = validator;
        }








        void OnDisconnected(ExceptionalEventArgs e)
        {
            if (Interlocked.Increment(ref disconnectsFired) > 1)
                return;

            if (writer != null) writer.Continue = false;
            if (reader != null) reader.Continue = false;

            try { writerThread.Abort(); } catch { }
            try { readerThread.Abort(); } catch { }

            WrapCallback(() => Disconnected?.Invoke(this, e));
            WrapCallback(() => callbackManager.SetExceptionForAll(new ClientDisconnectedException(e.Description, e.Exception)));
        }

        Task<object> QueueCommandAsTask(Command command, int streamId, int messageStreamId, bool requireConnected = true)
        {
            if (requireConnected && IsDisconnected)
                return CreateExceptedTask(new ClientDisconnectedException("disconnected"));

            var task = callbackManager.Create(command.InvokeId);
            writer.Queue(command, streamId, messageStreamId);
            return task;
        }

        public async Task ConnectAsync()
        {
            if (hasConnected)
                return;

            var client = CreateTcpClient();
            client.NoDelay = NoDelay;
            client.ReceiveTimeout = ReceiveTimeout;
            client.SendTimeout = SendTimeout;
            client.ExclusiveAddressUse = ExclusiveAddressUse;

            await client.ConnectAsync(uri.Host, uri.Port);
            var stream = await GetRtmpStreamAsync(client);


            var random = new Random();
            var randomBytes = new byte[1528];
            random.NextBytes(randomBytes);

            // write c0+c1
            var c01 = new Handshake()
            {
                Version = 3,
                Time = (uint)Environment.TickCount,
                Time2 = 0,
                Random = randomBytes
            };
            await Handshake.WriteAsync(stream, c01, true);

            // read s0+s1
            var s01 = await Handshake.ReadAsync(stream, true);

            // write c2
            var c2 = s01.Clone();
            c2.Time2 = (uint)Environment.TickCount;
            await Handshake.WriteAsync(stream, c2, false);

            // read s2
            var s2 = await Handshake.ReadAsync(stream, false);

            // handshake check
            if (!c01.Random.SequenceEqual(s2.Random) || c01.Time != s2.Time)
                throw new ProtocolViolationException();
            
            writer = new RtmpPacketWriter(new AmfWriter(stream, context), ObjectEncoding.Amf3);
            reader = new RtmpPacketReader(new AmfReader(stream, context));
            reader.EventReceived += EventReceivedCallback;
            reader.Disconnected += OnPacketProcessorDisconnected;
            writer.Disconnected += OnPacketProcessorDisconnected;

            writerThread = new Thread(reader.ReadLoop) { IsBackground = true };
            readerThread = new Thread(writer.WriteLoop) { IsBackground = true };

            writerThread.Start();
            readerThread.Start();

            // call `connect`
            var connectResult = await ConnectInvokeAsync(null, null, uri.ToString());
            object cId;
            if (connectResult.TryGetValue("clientId", out cId))
                ClientId = cId as string;

            hasConnected = true;
        }

        public void Close()
        {
            OnDisconnected(new ExceptionalEventArgs("disconnected"));
        }

        TcpClient CreateTcpClient()
        {
            return LocalEndPoint == null
                ? new TcpClient()
                : new TcpClient(LocalEndPoint);
        }

        async Task<Stream> GetRtmpStreamAsync(TcpClient client)
        {
            var stream = client.GetStream();
            switch (uri.Scheme)
            {
                case "rtmp":
                    return stream;
                case "rtmps":
                    var ssl = new SslStream(stream, false, validator);
                    await ssl.AuthenticateAsClientAsync(uri.Host);
                    return ssl;
                default:
                    throw new ArgumentException($"the scheme '{uri.Scheme}' is not supported");
            }
        }

        void OnPacketProcessorDisconnected(object sender, ExceptionalEventArgs args)
        {
            OnDisconnected(args);
        }

        void EventReceivedCallback(object sender, EventReceivedEventArgs e)
        {
            switch (e.Event.MessageType)
            {
                case MessageType.UserControlMessage:
                    var m = (UserControlMessage)e.Event;
                    if (m.EventType == UserControlMessageType.PingRequest)
                        WriteProtocolControlMessage(new UserControlMessage(UserControlMessageType.PingResponse, m.Values));
                    break;

                case MessageType.DataAmf3:
#if DEBUG
                    // have no idea what the contents of these packets are.
                    // study these packets if we receive them.
                    System.Diagnostics.Debugger.Break();
#endif
                    break;

                case MessageType.CommandAmf3:
                case MessageType.DataAmf0:
                case MessageType.CommandAmf0:
                    var command = (Command)e.Event;
                    var call = command.MethodCall;

                    var param = call.Parameters.Length == 1 ? call.Parameters[0] : call.Parameters;
                    switch (call.Name)
                    {
                        case "_result":
                            // unwrap Flex class, if present
                            var ack = param as AcknowledgeMessage;
                            callbackManager.SetResult(command.InvokeId, ack != null ? ack.Body : param);
                            break;

                        case "_error":
                            // unwrap Flex class, if present
                            var error = param as ErrorMessage;
                            callbackManager.SetException(command.InvokeId, error != null ? new InvocationException(error) : new InvocationException());
                            break;

                        case "receive":
                            var message = param as AsyncMessage;
                            if (message == null)
                                break;

                            object subtopicObject;
                            message.Headers.TryGetValue(AsyncMessageHeaders.Subtopic, out subtopicObject);

                            var dsSubtopic = subtopicObject as string;
                            var clientId = message.ClientId;
                            var body = message.Body;

                            WrapCallback(() => MessageReceived?.Invoke(this, new MessageReceivedEventArgs(clientId, dsSubtopic, body)));
                            break;

                        case "onstatus":
                            System.Diagnostics.Debug.Print("received status");
                            break;

                        default:
#if DEBUG
                            System.Diagnostics.Debug.Print($"unknown rtmp command: {call.Name}");
                            System.Diagnostics.Debugger.Break();
#endif
                            break;
                    }

                    break;
            }
        }




        public Task<T> InvokeAsync<T>(string method, object argument)
        {
            return InvokeAsync<T>(method, new[] { argument });
        }

        public async Task<T> InvokeAsync<T>(string method, object[] arguments)
        {
            var result = await QueueCommandAsTask(new InvokeAmf0
            {
                MethodCall = new Method(method, arguments),
                InvokeId = GetNextInvokeId()
            }, 3, 0);
            return (T)MiniTypeConverter.ConvertTo(result, typeof(T));
        }

        public Task<T> InvokeAsync<T>(string endpoint, string destination, string method, object argument)
        {
            return InvokeAsync<T>(endpoint, destination, method, new[] { argument });
        }

        public async Task<T> InvokeAsync<T>(string endpoint, string destination, string method, object[] arguments)
        {
            if (objectEncoding != ObjectEncoding.Amf3)
                throw new NotSupportedException("Flex RPC requires AMF3 encoding.");

            var remotingMessage = new RemotingMessage
            {
                ClientId = Guid.NewGuid().ToString("D"),
                Destination = destination,
                Operation = method,
                Body = arguments,
                Headers = new Dictionary<string, object>()
                {
                    { FlexMessageHeaders.Endpoint, endpoint },
                    { FlexMessageHeaders.FlexClientId, ClientId ?? "nil" }
                }
            };

            var result = await QueueCommandAsTask(new InvokeAmf3()
            {
                InvokeId = GetNextInvokeId(),
                MethodCall = new Method(null, new object[] { remotingMessage })
            }, 3, 0);
            return (T)MiniTypeConverter.ConvertTo(result, typeof(T));
        }










        async Task<AsObject> ConnectInvokeAsync(string pageUrl, string swfUrl, string tcUrl)
        {
            var connect = new InvokeAmf0
            {
                MethodCall = new Method("connect", new object[0]),
                ConnectionParameters = new AsObject
                {
                    { "pageUrl",           pageUrl                },
                    { "objectEncoding",    (double)objectEncoding },
                    { "capabilities",      15                     },
                    { "audioCodecs",       1639                   },
                    { "flashVer",          "WIN 9,0,115,0"        },
                    { "swfUrl",            swfUrl                 },
                    { "videoFunction",     1                      },
                    { "fpad",              false                  },
                    { "videoCodecs",       252                    },
                    { "tcUrl",             tcUrl                  },
                    { "app",               null                   }
                },
                InvokeId = GetNextInvokeId()
            };
            return (AsObject)await QueueCommandAsTask(connect, 3, 0, requireConnected: false);
        }

        public async Task<bool> SubscribeAsync(string endpoint, string destination, string subtopic, string clientId)
        {
            var message = new CommandMessage
            {
                ClientId = clientId,
                CorrelationId = null,
                Operation = CommandOperation.Subscribe,
                Destination = destination,
                Headers = new Dictionary<string, object>()
                {
                    { FlexMessageHeaders.Endpoint, endpoint },
                    { FlexMessageHeaders.FlexClientId, clientId },
                    { AsyncMessageHeaders.Subtopic, subtopic }
                }
            };

            return await InvokeAsync<string>(null, message) == "success";
        }

        public async Task<bool> UnsubscribeAsync(string endpoint, string destination, string subtopic, string clientId)
        {
            var message = new CommandMessage
            {
                ClientId = clientId,
                CorrelationId = null,
                Operation = CommandOperation.Unsubscribe,
                Destination = destination,
                Headers = new Dictionary<string, object>()
                {
                    { FlexMessageHeaders.Endpoint, endpoint },
                    { FlexMessageHeaders.FlexClientId, clientId },
                    { AsyncMessageHeaders.Subtopic, subtopic }
                }
            };

            return await InvokeAsync<string>(null, message) == "success";
        }

        public async Task<bool> LoginAsync(string username, string password)
        {
            var message = new CommandMessage
            {
                ClientId = ClientId,
                Destination = string.Empty, // destination must not be null to work on some servers
                Operation = CommandOperation.Login,
                Body = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}")),
            };

            return await InvokeAsync<string>(null, message) == "success";
        }

        public Task LogoutAsync()
        {
            var message = new CommandMessage
            {
                ClientId = ClientId,
                Destination = string.Empty,
                Operation = CommandOperation.Logout
            };
            return InvokeAsync<object>(null, message);
        }

        public void SetChunkSize(int size)
        {
            WriteProtocolControlMessage(new ChunkSize(size));
        }

        public Task PingAsync()
        {
            var message = new CommandMessage
            {
                ClientId = ClientId,
                Destination = string.Empty,
                Operation = CommandOperation.ClientPing
            };
            return InvokeAsync<object>(null, message);
        }

        void WriteProtocolControlMessage(RtmpEvent @event)
        {
            writer.Queue(@event, 2, 0);
        }

        int GetNextInvokeId()
        {
            // interlocked.increment wraps overflows
            return Interlocked.Increment(ref invokeId);
        }

        void WrapCallback(Action action)
        {
            try
            {
                try { action(); }
                catch (Exception ex) { CallbackException?.Invoke(this, ex); }
            }
#if DEBUG && BREAK_ON_EXCEPTED_CALLBACK
            catch (Exception unhandled)
            {
                System.Diagnostics.Debug.Print("UNHANDLED EXCEPTION IN CALLBACK: {0}: {1} @ {2}", unhandled.GetType(), unhandled.Message, unhandled.StackTrace);
                System.Diagnostics.Debugger.Break();
            }
#else
            catch { }
#endif
        }

        static Task<object> CreateExceptedTask(Exception exception)
        {
            var source = new TaskCompletionSource<object>();
            source.SetException(exception);
            return source.Task;
        }




        #region handshake

        const int HandshakeRandomSize = 1528;

        // size for c0, c1, s1, s2 packets. c0 and s0 are 1 byte each.
        const int HandshakeSize = HandshakeRandomSize + 4 + 4;

        struct Handshake
        {
            // C0/S0 only
            public byte Version;

            // C1/S1/C2/S2
            public uint Time;
            // in C1/S1, MUST be zero. in C2/S2, time at which C1/S1 was read.
            public uint Time2;
            public byte[] Random;

            public Handshake Clone()
            {
                return new Handshake()
                {
                    Version = Version,
                    Time = Time,
                    Time2 = Time2,
                    Random = Random
                };
            }

            public static async Task<Handshake> ReadAsync(Stream stream, bool readVersion)
            {
                var size = HandshakeSize + (readVersion ? 1 : 0);
                var buffer = await StreamHelper.ReadBytesAsync(stream, size);

                using (var reader = new AmfReader(new MemoryStream(buffer), null))
                {
                    return new Handshake()
                    {
                        Version = readVersion ? reader.ReadByte() : default(byte),
                        Time = reader.ReadUInt32(),
                        Time2 = reader.ReadUInt32(),
                        Random = reader.ReadBytes(HandshakeRandomSize)
                    };
                }
            }

            public static Task WriteAsync(Stream stream, Handshake h, bool writeVersion)
            {
                using (var memoryStream = new MemoryStream())
                using (var writer = new AmfWriter(memoryStream, null))
                {
                    if (writeVersion)
                        writer.WriteByte(h.Version);

                    writer.WriteUInt32(h.Time);
                    writer.WriteUInt32(h.Time2);
                    writer.WriteBytes(h.Random);

                    var buffer = memoryStream.ToArray();
                    return stream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
        }

        #endregion
    }
}