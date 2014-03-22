using System.Linq;

namespace RtmpSharp.IO.ObjectWrappers
{
    class AsObjectWrapper : IObjectWrapper
    {
        static readonly ClassDescription EmptyClassDescription = new AsObjectClassDescription(string.Empty, new IMemberWrapper[] { }, false, true);

        readonly SerializationContext serializationContext;

        public AsObjectWrapper(SerializationContext serializationContext)
        {
            this.serializationContext = serializationContext;
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
            if (aso.IsTyped)
            {
                var members = aso.Select(x => (IMemberWrapper)new AsObjectMemberWrapper(x.Key)).ToArray();
                var typeName = serializationContext.GetAlias(aso.TypeName);
                return new AsObjectClassDescription(typeName, members, false, false);
            }
            return EmptyClassDescription;
        }

        class AsObjectClassDescription : ClassDescription
        {
            internal AsObjectClassDescription(string name, IMemberWrapper[] members, bool externalizable, bool dynamic) : base(name, members, externalizable, dynamic)
            {
            }
            
            public override bool TryGetMember(string name, out IMemberWrapper memberWrapper)
            {
                memberWrapper = new AsObjectMemberWrapper(name);
                return true;
            }
        }

        class AsObjectMemberWrapper : IMemberWrapper
        {
            public string Name { get; private set; }
            public string SerializedName { get { return Name; }}

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
