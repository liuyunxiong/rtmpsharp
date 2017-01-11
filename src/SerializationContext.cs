using System;
using RtmpSharp.Infos;

namespace RtmpSharp
{
    public class SerializationContext
    {
        // if true, then we fall back to deserializing unregistered objects into asobjects. otherwise, trying to
        // deserialize an object with unknown types will throw an exception.
        public bool AsObjectFallback = true;

        // for rtmp connections, specifies the largest allocation allowed when reading from a remote peer. this will be
        // the largest buffer size we allocate in order to read and deserialize packets and object trees. if a remote
        // peer attempts to send us a packet or object that is larger than this value, an exception is thrown and the
        // connection is closed.
        public int MaximumReadAllocation = 4192;


        readonly TypeRegistry registry;
        readonly ObjectInfo   infos;


        public SerializationContext(params Type[] types)
        {
            infos    = new ObjectInfo(this);
            registry = new TypeRegistry();

            foreach (var type in types)
                registry.RegisterType(type);
        }


        public object      CreateInstance(string name)   => CreateOrNull(name) ?? throw new InvalidOperationException($"the type \"{name}\" hasn't been registered with this context");

        internal object    CreateOrNull(string name)     => registry.CreateOrNull(name);
        internal bool      HasConcreteType(string name)  => registry.Exists(name);
        internal string    GetCanonicalName(string name) => registry.CanonicalName(name);
        internal ClassInfo GetClassInfo(object obj)      => infos.GetClassInfo(obj) ?? throw new InvalidOperationException("couldn't get class description for that object");


        internal void RequestReadAllocation(int requested)
        {
            if (requested > MaximumReadAllocation)
                throw new InvalidOperationException("attempted to allocate more than the maximum read allocation amount");
        }
    }
}