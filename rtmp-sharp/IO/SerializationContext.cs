using System;
using System.Collections.Generic;

namespace RtmpSharp.IO
{
    public class SerializationContext
    {
        internal SerializerObjectFactory SerializerObjectFactory = new SerializerObjectFactory();
        internal ObjectWrapperFactory ObjectWrapperFactory;

        public SerializationContext()
        {
            SerializerObjectFactory = new SerializerObjectFactory();
            ObjectWrapperFactory = new ObjectWrapperFactory(this);
        }

        public SerializationContext(IEnumerable<Type> types) : this()
        {
            foreach (var type in types)
                Register(type);
        }

        public void Register(Type type)
        {
            SerializerObjectFactory.Register(type);
        }

        public void RegisterAlias(Type type, string alias, bool canonical)
        {
            SerializerObjectFactory.RegisterAlias(type, alias, canonical);
        }
    }
}
