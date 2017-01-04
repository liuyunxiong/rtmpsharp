# RtmpSharp (v0.2)

A fast, lightweight, data-oriented rtmp + rtmps client library for .NET Desktop and .NET Core. Used in many high-
traffic websites and systems, especially around the game streaming and League of Legends spheres.

## Example Usage

```csharp
var context = new SerializationContext();
var options = new RtmpClient.Options()
{
    // required parameters:
    Url      = "rtmp://ingress.winky.com:1234",
    Context  = context,

    // optional parameters:
    AppName     = "demo-app",                                  // optional app name, passed to the remote server during connect.
    PageUrl     = "https://example.com/rtmpsharp.demo",        // optional page url, passed to the remote server during connect.
    SwfUrl      = "",                                          // optional swf url,  passed to the remote server during connect.
    ChunkLength = 4192,                                        // optional outgoing rtmp chunk length.
    Validate    = (sender, certificate, chain, errors) => true // optional certificate validation callback. used only in tls connections.
};

// connect to the winky and invoke the `musical.search` service.
var client = RtmpClient.ConnectAsync(options);
var songs  = await client.InvokeAsync<string[]>("musical", "search", new { name = "kiss me" });
```

## The Serliazation Context

The `SerializationContext` isolates different serialization domains, and holds information mappings for type
serialization. this allows you to have separate serialization domains for different services and not worry about
namespace collisions: twitchtv + youtube may both expose an rtmp interface, but have slightly different definitions for
what constitutes a video object.

The `SerializationContext` constructor accepts an optional array of types that the instance should serialize into their
respective concrete types. If `rtmpsharp` receives a type that isn't registered, it will by default deserialize that
object into an `AsObject`. If you don't like this, and want to fail deserialization, then turn `AsObjectFallback` off.
So if you do not pass it any types, then all objects will be deserialized into anonymous `AsObject`s.

```csharp
// constructor definition:
//     new SerializationContext(params Type[] types);
```

Sometimes it might be easier to use the DLR, and `AsObject`s natively support that:

```csharp
dynamic d = await client.InvokeAsync<dynamic>("greeter-service", "greet", "hello!");

Console.WriteLine(d.items[0].greeting)
// => hello!
```

## Type Annotations

By default, `rtmpsharp` will serialize all public fields and properties using their native CLR names. To instruct
`rtmpsharp` to use a different name for serialization, simply annotate the interested types or members with the
`RtmpSharp` attribute. Ignore a field by annotating it with `RtmpIgnore`.

```csharp
namespace Client
{
    // without annotation: `Client.WinkyServiceStatus`
    // with annotation:    `org.winky.ServiceStatus`
    [RtmpSharp("org.winky.ServiceStatus")]
    public class WinkyServiceStatus
    {
        // without annotation: `Difficulty`
        // with annotation:    `hardness`
        [RtmpSharp("hardness")]
        public string Difficulty;

        // ignored from serialization
        [RtmpIgnore("gameId")]
        public int MatchId;
    }
}
```

## Changes From v0.1

`rtmpsharp` v0.2 is a significant upgrade from v0.1 - a large portion of the code base has been revised and rewritten.

With v0.2, we've consistently seen large and significant (> 100x) improvements in throughput, as well as improvements in
latency, GC pressure, and memory consumption. These benefits are especially seen if you are using `rtmpsharp` at scale,
whether for large object graphs, or for tiny objects streamed in a high frequency firehouse.

v0.2 also improves how it handles of disconnections and is a little more intelligent in serializing objects, and in
speaking the RTMP protocol. In addition, v0.2 now spins up zero threads (down from one reader thread and one writer
thread for every connection), so it is finally feasible to start up distinct 10,000 concurrent connections on a single
machine.

## License

- `rtmpsharp` is MIT licensed
- Feel free to use it in any way you wish
- However, please contribute any improvements you make back to this repository