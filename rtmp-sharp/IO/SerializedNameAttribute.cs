using System;

namespace RtmpSharp.IO
{
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Interface | AttributeTargets.Field | AttributeTargets.Property, AllowMultiple = true)]
    public sealed class SerializedNameAttribute : Attribute
    {
        public string SerializedName { get; set; }
        public bool Canonical { get; set; }

        public SerializedNameAttribute(string serializedName)
        {
            SerializedName = serializedName;
            Canonical = true;
        }
    }
}
