using RtmpSharp.IO.AMF3;
using RtmpSharp.IO.ObjectWrappers;
using System;
using System.Collections.Generic;

namespace RtmpSharp.IO
{
    class ObjectWrapperFactory
    {
        static readonly string ExternalizableTypeName = typeof(IExternalizable).FullName;

        readonly SerializationContext context;

        readonly IObjectWrapper defaultWrapper;
        readonly Dictionary<Type, IObjectWrapper> wrappers = new Dictionary<Type, IObjectWrapper>();
        
        public ObjectWrapperFactory(SerializationContext context)
        {
            this.context = context;
            
            defaultWrapper = new BasicObjectWrapper(context);

            wrappers[typeof(AsObject)] = new AsObjectWrapper(context);
            wrappers[typeof(IExternalizable)] = new ExternalizableWrapper(context);
            wrappers[typeof(Exception)] = new ExceptionWrapper(context);
        }

        public IObjectWrapper GetInstance(Type type)
        {
            if (type.GetInterface(ExternalizableTypeName, true) != null)
                return wrappers[typeof(IExternalizable)];

            IObjectWrapper wrapper;
            if (wrappers.TryGetValue(type, out wrapper))
                return wrapper;

            foreach (var entry in wrappers)
            {
                if (type.IsSubclassOf(entry.Key))
                    return entry.Value;
            }
            
            return defaultWrapper;
        }

        public ClassDescription GetClassDescription(object obj)
        {
            var instance = GetInstance(obj.GetType());
            return instance.GetClassDescription(obj);
        }

        public ClassDescription GetClassDescription(Type type, object obj)
        {
            var instance = GetInstance(type);
            return instance.GetClassDescription(obj);
        }
    }
}
