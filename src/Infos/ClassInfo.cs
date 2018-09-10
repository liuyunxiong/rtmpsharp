using System;

namespace RtmpSharp.Infos
{
    interface IObjectInfo
    {
        bool      IsExternalizable(object instance);
        bool      IsDynamic(object instance);
        ClassInfo GetClassInfo(object instance);
    }

    class ClassInfo
    {
        public string        Name             { get; }
        public IMemberInfo[] Members          { get; }
        public bool          IsExternalizable { get; }
        public bool          IsDynamic        { get; }
        public bool          IsTyped          => !string.IsNullOrEmpty(Name);

        public virtual bool TryGetMember(string name, out IMemberInfo member)
            => throw new NotImplementedException();

        public ClassInfo(string name, IMemberInfo[] members, bool externalizable, bool dynamic)
        {
            Name             = name;
            Members          = members;
            IsExternalizable = externalizable;
            IsDynamic        = dynamic;
        }
    }

    interface IMemberInfo
    {
        string Name      { get; }
        string LocalName { get; }
        object GetValue(object instance);
        void   SetValue(object instance, object value);
    }
}
