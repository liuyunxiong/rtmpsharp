using System;

namespace RtmpSharp.IO
{
    class ClassDescription
    {
        public string Name { get; }
        public IMemberWrapper[] Members { get; }
        public bool IsExternalizable { get; }
        public bool IsDynamic { get; }
        public bool IsTyped => !string.IsNullOrEmpty(Name);

        public virtual bool TryGetMember(string name, out IMemberWrapper member) { throw new NotImplementedException(); }

        internal ClassDescription(string name, IMemberWrapper[] members, bool externalizable, bool dynamic)
        {
            this.Name = name;
            this.Members = members;
            this.IsExternalizable = externalizable;
            this.IsDynamic = dynamic;
        }
    }
}
