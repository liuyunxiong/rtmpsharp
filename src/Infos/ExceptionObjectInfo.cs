using System.Linq;
using Hina.Linq;

namespace RtmpSharp.Infos
{
    class ExceptionObjectInfo : BasicObjectInfo
    {
        static readonly string[] ExcludedMembers = { "HelpLink", "HResult", "Source", "StackTrace", "TargetSite" };

        public ExceptionObjectInfo(SerializationContext context)
            : base(context) { }

        public override ClassInfo GetClassInfo(object instance)
        {
            var klass = base.GetClassInfo(instance);

            return new ClassInfo(
                name:           klass.Name,
                members:        klass.Members.FilterArray(x => !ExcludedMembers.Contains(x.LocalName)),
                externalizable: klass.IsExternalizable,
                dynamic:        klass.IsDynamic);
        }
    }
}
