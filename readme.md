# rtmp-sharp

A fast, lightweight library RTMP(S) client library for .NET. It's known to be in many high-traffic
websites especially around the video game streaming and League of Legends spheres.

## Example usage

#### Simple server connection + make a call

```csharp
var client = new RtmpClient(
	new Uri("rtmps://ingress.winky.com"),
	new SerializationContext(),
	ObjectEncoding.Amf3);

// connect to the server
await client.ConnectAsync();

// call a remote service
var songs = await client.InvokeAsync<string[]>("musicalService", "findSongs");
```

## Serialization domains

This section is only relevant if you are *serializing and deserializing typed
objects*.

The `SerializationContext` isolates different serialization domains to prevent
attacks from untrusted servers. It also negates issues that occur when there
is only a single global context: if two different domains (eg, twitch.tv and
winky.com) both have `ServiceStatus` objects, then it would be impossible
to tell which one to use. The `SerializationContext` allows us to specify
different objects for each context.

```csharp
// method one: specify the types in the constructor
var context = new SerializationContext(types);

// method two: add types in a method call
context.Register(type);
```

By default, the serialization context will deserialize unregistered objects to
anonymous types; if you have an empty context, all objects will be
deserialized to anonymous types.

If you don't need statically typed objects, you can use dynamic objects for
all objects instead:


```csharp
dynamic d = await client.InvokeAsync<dynamic>(...)
```

#### Object annotations

By default, `rtmp-sharp` uses an CLR type and field names for serialization.
If you need to use a different type name on the network, you can annotate your
code with the `SerializedName` attribute.

```csharp
namespace Client
{
    // Without annotation: `Client.WinkyServiceStatus`
    // With annotation: `org.winky.ServiceStatus`
    [Serializable]
    [SerializedName("org.winky.ServiceStatus")]
    public class WinkyServiceStatus
    {
        // Without annotation: `Difficulty`
        // With annotation: `hardness`
        [SerializedName("hardness")]
        public string Difficulty;

        [SerializedName("gameId")]
        public int MatchId;
    }
}
```

## Issues

When time allows, I'll fix these.

- `rtmp-sharp` spawns two threads per `RtmpClient` - a reader and writer thread
- flash shared objects aren't implemented
- video and audio decoding isn't implemented

## License

- `rtmp-sharp` is MIT licensed
- Feel free to use it however you want
- Please contribute any improvements you make back to this repository
