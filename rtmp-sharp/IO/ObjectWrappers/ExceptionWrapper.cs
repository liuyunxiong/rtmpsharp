using System.Collections.Generic;
using System.Linq;

namespace RtmpSharp.IO.ObjectWrappers
{
    class ExceptionWrapper : BasicObjectWrapper
    {
        static readonly HashSet<string> ExcludedMembers = new HashSet<string>()
        {
            "HelpLink",
            "HResult",
            "Source",
            "StackTrace",
            "TargetSite"
        };

        public ExceptionWrapper(SerializationContext serializationContext) : base(serializationContext)
        {
        }

        public override ClassDescription GetClassDescription(object obj)
        {
            var classDefinition = base.GetClassDescription(obj);
            return new ClassDescription(
                classDefinition.Name,
                classDefinition.Members.Where(x => !ExcludedMembers.Contains(x.Name)).ToArray(),
                classDefinition.IsExternalizable,
                classDefinition.IsDynamic);
        }
    }
}
