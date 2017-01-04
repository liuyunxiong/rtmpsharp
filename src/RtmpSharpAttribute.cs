using System;
using System.Linq;

namespace RtmpSharp
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = false)]
    public sealed class RtmpSharpAttribute : Attribute
    {
        public static readonly RtmpSharpAttribute Empty = new RtmpSharpAttribute();

        public string   CanonicalName;
        public string[] Names;

        public RtmpSharpAttribute(params string[] names)
        {
            Names = names;
            CanonicalName = names.FirstOrDefault();
        }
    }
}
