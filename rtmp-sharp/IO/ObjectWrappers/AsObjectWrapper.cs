using System.Linq;

namespace RtmpSharp.IO.ObjectWrappers
{
    class AsObjectWrapper : IObjectWrapper
    {
        static readonly ClassDescription EmptyClassDescription = new AsObjectClassDescription(string.Empty, new IMemberWrapper[] { }, false, true);

        readonly SerializationContext context;

        public AsObjectWrapper(SerializationContext serializationContext)
        {
            this.context = serializationContext;
        }

        public bool GetIsDynamic(object instance)
        {
            return ((AsObject)instance).IsTyped;
        }

        public bool GetIsExternalizable(object instance)
        {
            return false;
        }

        public ClassDescription GetClassDescription(object obj)
        {
            var aso = (AsObject)obj;
            if (!aso.IsTyped)
                return EmptyClassDescription;

            var members = aso.Select(x => new AsObjectMemberWrapper(x.Key)).Cast<IMemberWrapper>().ToArray();
            var typeName = context.GetAlias(aso.TypeName);
            return new AsObjectClassDescription(typeName, members, false, false);
        }

        class AsObjectClassDescription : ClassDescription
        {
            internal AsObjectClassDescription(string name, IMemberWrapper[] members, bool externalizable, bool dynamic) : base(name, members, externalizable, dynamic)
            {
            }
            
            public override bool TryGetMember(string name, out IMemberWrapper member)
            {
                member = new AsObjectMemberWrapper(name);
                return true;
            }
        }

        class AsObjectMemberWrapper : IMemberWrapper
        {
            public string Name { get; }
            public string SerializedName => Name;

            public AsObjectMemberWrapper(string name)
            {
                Name = name;
            }

            public object GetValue(object instance)
            {
                var aso = (AsObject)instance;
                return aso[Name];
            }

            public void SetValue(object instance, object value)
            {
                var aso = (AsObject)instance;
                aso[Name] = value;
            }
        }
    }
}
