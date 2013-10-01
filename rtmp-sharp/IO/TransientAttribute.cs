using System;

namespace RtmpSharp.IO
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
    public sealed class TransientAttribute : Attribute
    {
    }
}
