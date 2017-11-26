# rtmp-sharp (v0.3) [![NuGet](https://img.shields.io/nuget/v/rtmpsharp.svg?style=flat-square)](https://www.nuget.org/packages/rtmpsharp)

`rtmp-sharp` is a fast and lightweight data-oriented RTMP(S) library for .NET Desktop and .NET Core. [Install from
NuGet](https://www.nuget.org/packages/rtmpsharp), or compile from source.

This library is currently stable and used in production, serving more than 1 billion requests every month, as well as in
many other large sites and applications, especially in the video game streaming and League of Legends spheres.

Commercial support is optionally available: email [hello@unyaa.com](mailto:hello@unyaa.com) for more information.

## Example Usage

```csharp
var context = new SerializationContext();
var options = new RtmpClient.Options()
{
    // required parameters:
    Url         = "rtmp://ingress.winky.com:1234",
    Context     = context,

    // optional parameters:
    AppName     = "demo-app",                                  // optional app name, passed to the remote server during connect.
    PageUrl     = "https://example.com/rtmpsharp/demo.html",   // optional page url, passed to the remote server during connect.
    SwfUrl      = "",                                          // optional swf url,  passed to the remote server during connect.
    ChunkLength = 4192,                                        // optional outgoing rtmp chunk length.
    Validate    = (sender, certificate, chain, errors) => true // optional certificate validation callback. used only in tls connections.
};

var client = await RtmpClient.ConnectAsync(options);
var exists = await client.InvokeAsync<bool>("storage", "exists", new { name = "music.pdf" });
```

## The Serialization Context

The `SerializationContext` isolates different serialization domains, and holds information mappings for type
serialization. this allows you to have separate serialization domains for different services  and not worry about
namespace collisions: twitchtv + youtube may both expose an rtmp interface, but have slightly different definitions for
what constitutes a video object.

The `SerializationContext` constructor accepts an optional array of types that the instance should serialize into their
respective concrete types. If `rtmp-sharp` receives a type that isn't registered, it will by default deserialize that
object into an `AsObject`. If you don't like this, and want to fail deserialization, then turn `AsObjectFallback` off.
So if you do not pass it any types, then all objects will be deserialized into anonymous `AsObject`s.

```csharp
// constructor definition:
//     new SerializationContext(params Type[] types);
```

`AsObjects` support the DLR and can thus be used with `dynamic` - this may be more convenient for some use cases.

```csharp
dynamic d = await client.InvokeAsync<dynamic>("greeter-service", "greet", "hello!");

Console.WriteLine(d.items[0].greeting)
// => hello!
```

## Type Annotations

By default, `rtmp-sharp` will serialize all public fields and public properties using their field names. You may
instruct `rtmp-sharp` to use a different name for serialization by simply annotating the interested types or members
with the `RtmpSharp` attribute. Ignore a field by annotating it with `RtmpIgnore`.

```csharp
namespace Winky
{
    // without annotation: `Winky.StorageEntry`
    // with annotation:    `org.winky.StorageEntry`
    [RtmpSharp("org.winky.StorageEntry")]
    public class StorageEntry
    {
        // without the `RtmpSharp` annotation, this field would be encoded as `Name` over the wire. with this
        // annotation, it is instead encoded as the field `display_name`.
        [RtmpSharp("display_name")]
        public string Name;

        // this field does not have any annotations, but because it is a public field, it will still be serialized.
        public byte[] Hash;

        // this attribute directs `rtmp-sharp` to ignore this field: it will not be considered during serialization and
        // deserialization.
        [RtmpIgnore]
        public int State;
    }
}
```

## Detailed Documentation

Detailed API docs and examples are coming soon; they're temporarily blocked on the open sourcing of some internal
documentation generation tooling.

## Changes From v0.1

`rtmp-sharp` v0.2 is a significant upgrade from v0.1 - a large portion of the code base has been revised and rewritten.

With v0.2, we've consistently seen large and significant (> 100x) improvements in throughput, as well as improvements in
message latency, GC pressure, and memory consumption. These benefits are especially visible if you are using
`rtmp-sharp` at scale, whether it is in serializing millions of large object graphs, or for tiny objects streamed in a
gigantic firehose.

v0.2 also improves how it handles disconnections, is a little more intelligent in serializing objects, and in speaking
the RTMP protocol. Overall, this means slightly reduced sizes for serialized payloads, slightly reduced network usage,
and slightly greater compatibility with other RTMP servers. In addition, v0.2 no longer spins up dedicated connection
worker threads (down from one reader thread and one writer thread for every connection), so it is finally feasible to
start up distinct 10,000 concurrent connections on a single machine.

Some classes have been moved into other namespaces to match their semantic meaning, rather than matching the unnatural
placement of RTMP libraries.

## License

- This library is MIT licensed
- Feel free to use it in any way you wish
- Please contribute improvements back to this repository!