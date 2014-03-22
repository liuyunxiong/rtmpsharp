using System;

namespace RtmpSharp.IO
{
    [Flags]
    public enum FallbackStrategy
    {
        DynamicObject,
        Exception
    }
}
