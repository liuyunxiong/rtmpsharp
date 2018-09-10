using Hina.Linq;

namespace RtmpSharp.Infos
{
    class AsObjectInfo : IObjectInfo
    {
        readonly SerializationContext context;

        public AsObjectInfo(SerializationContext context) => this.context = context;
        public bool IsDynamic(object instance)            => ((AsObject)instance).IsTyped;
        public bool IsExternalizable(object instance)     => false;

        public ClassInfo GetClassInfo(object instance)
        {
            var obj = (AsObject)instance;

            if (obj.IsTyped)
            {
                return new AsObjectClassInfo(
                    name:           context.GetCanonicalName(obj.TypeName),
                    members:        obj.MapArray(x => new AsObjectMemberInfo(x.Key)),
                    externalizable: false,
                    dynamic:        false);
            }

            return AsObjectClassInfo.Empty;
        }

        class AsObjectClassInfo : ClassInfo
        {
            public static readonly ClassInfo Empty = new AsObjectClassInfo(
                name:           string.Empty,
                members:        new IMemberInfo[0],
                externalizable: false,
                dynamic:        true);

            public AsObjectClassInfo(string name, IMemberInfo[] members, bool externalizable, bool dynamic) : base(name, members, externalizable, dynamic)
            {
            }

            public override bool TryGetMember(string name, out IMemberInfo member)
            {
                member = new AsObjectMemberInfo(name);
                return true;
            }
        }

        class AsObjectMemberInfo : IMemberInfo
        {
            public string Name => LocalName;
            public string LocalName { get; }

            public AsObjectMemberInfo(string name) => LocalName = name;

            public object GetValue(object instance)               => ((AsObject)instance)[LocalName];
            public void   SetValue(object instance, object value) => ((AsObject)instance)[LocalName] = value;
        }
    }
}
