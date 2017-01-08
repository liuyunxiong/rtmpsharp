using System;
using RtmpSharp.IO.AMF3;
using RtmpSharp.Infos;

namespace RtmpSharp
{
    // provides information about object types for some rtmp serialization context. provides types, names, and
    // facilities to get and set values of instances of those objects.
    class ObjectInfo
    {
        readonly SerializationContext context;

        readonly IObjectInfo basic;
        readonly IObjectInfo externalizable;
        readonly IObjectInfo asObject;
        readonly IObjectInfo exception;

        public ObjectInfo(SerializationContext context)
        {
            this.context = context;

            basic          = new BasicObjectInfo(context);
            asObject       = new AsObjectInfo(context);
            externalizable = new ExternalizableObjectInfo(context);
            exception      = new ExceptionObjectInfo(context);
        }

        public IObjectInfo GetInstance(object obj)
        {
            if (obj is IExternalizable) return basic;
            if (obj is AsObject)        return asObject;
            if (obj is Exception)       return exception;

            return basic;
        }

        public ClassInfo GetClassInfo(object obj) => GetInstance(obj).GetClassInfo(obj);
    }
}
