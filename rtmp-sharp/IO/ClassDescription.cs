using System;

namespace RtmpSharp.IO
{
    class ClassDescription
    {
        public string Name { get; private set; }
        public IMemberWrapper[] Members { get; private set; }
        public bool IsExternalizable { get; private set; }
        public bool IsDynamic { get; private set; }
        public bool IsTyped { get { return !string.IsNullOrEmpty(Name); } }
        
        public virtual bool TryGetMember(string name, out IMemberWrapper memberWrapper) { throw new NotImplementedException(); }

        internal ClassDescription(string name, IMemberWrapper[] members, bool externalizable, bool dynamic)
        {
            this.Name = name;
            this.Members = members;
            this.IsExternalizable = externalizable;
            this.IsDynamic = dynamic;
        }
    }
}
