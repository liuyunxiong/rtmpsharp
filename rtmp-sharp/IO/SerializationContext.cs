using System;
using System.Collections.Generic;

namespace RtmpSharp.IO
{
    public class SerializationContext
    {
        // Specifies the course of action to take when a type cannot be serialized to a typed
        // object (because it has not been registered)
        readonly FallbackStrategy fallbackStrategy;
        readonly SerializerObjectFactory serializerObjectFactory;
        readonly ObjectWrapperFactory objectWrapperFactory;



        public SerializationContext()
        {
            this.fallbackStrategy = FallbackStrategy.DynamicObject;
            this.serializerObjectFactory = new SerializerObjectFactory();
            this.objectWrapperFactory = new ObjectWrapperFactory(this);
        }

        public SerializationContext(IEnumerable<Type> types) : this()
        {
            foreach (var type in types)
                Register(type);
        }

        public SerializationContext(FallbackStrategy fallbackStrategy) : this()
        {
            this.fallbackStrategy = fallbackStrategy;
        }

        public SerializationContext(FallbackStrategy fallbackStrategy, IEnumerable<Type> types) : this(fallbackStrategy)
        {
            foreach (var type in types)
                Register(type);
        }


        
        public void Register(Type type) { serializerObjectFactory.Register(type); }
        public void RegisterAlias(Type type, string alias, bool canonical) { serializerObjectFactory.RegisterAlias(type, alias, canonical); }

        internal string GetAlias(string typeName) { return serializerObjectFactory.GetAlias(typeName); }

        internal bool CanCreate(string typeName) { return serializerObjectFactory.CanCreate(typeName); }
        internal bool CanCreate(Type type) { return serializerObjectFactory.CanCreate(type); }

        internal object Create(string typeName) { return serializerObjectFactory.Create(typeName); }
        internal object Create(Type type) { return serializerObjectFactory.Create(type); }



        internal ClassDescription GetClassDescription(object obj) { return objectWrapperFactory.GetClassDescription(obj); }
        internal ClassDescription GetClassDescription(Type type, object obj) { return objectWrapperFactory.GetClassDescription(type, obj); }



        internal DeserializationStrategy GetDeserializationStrategy(string typeName)
        {
            return CanCreate(typeName)
                ? DeserializationStrategy.TypedObject
                : GetFallbackDeserializationStrategy();
        }

        internal DeserializationStrategy GetDeserializationStrategy(Type type)
        {
            return CanCreate(type)
                ? DeserializationStrategy.TypedObject
                : GetFallbackDeserializationStrategy();
        }

        DeserializationStrategy GetFallbackDeserializationStrategy()
        {
            return fallbackStrategy == FallbackStrategy.DynamicObject
                ? DeserializationStrategy.DynamicObject
                : DeserializationStrategy.Exception;
        }
    }
}
