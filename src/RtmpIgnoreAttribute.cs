using System;

namespace RtmpSharp
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class RtmpIgnoreAttribute : Attribute
    {
    }
}
