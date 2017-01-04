using System;
using Hina;

namespace RtmpSharp.Infos
{
    class ExternalizableObjectInfo : IObjectInfo
    {
        readonly SerializationContext context;

        public ExternalizableObjectInfo(SerializationContext context) => this.context = context;
        public bool IsDynamic(object instance)                     => false;
        public bool IsExternalizable(object instance)              => false;

        public ClassInfo GetClassInfo(object instance)
        {
            var type = instance.GetType();
            
            return new ExternalizableClassInfo(
                name:           context.GetCanonicalName(type.FullName),
                members:        EmptyCollection<IMemberInfo>.Array,
                externalizable: true,
                dynamic:        false);
        }

        class ExternalizableClassInfo : ClassInfo
        {
            public ExternalizableClassInfo(string name, IMemberInfo[] members, bool externalizable, bool dynamic)
                : base(name, members, externalizable, dynamic) { }

            public override bool TryGetMember(string name, out IMemberInfo member) 
                => throw new InvalidOperationException("attempting to access member info for externalizable object");
        }
    }
}
