
namespace RtmpSharp.Net
{
    enum ChunkMessageHeaderType : byte
    {
        New = 0,
        SameSource = 1,
        TimestampAdjustment = 2,
        Continuation = 3
    }
}
