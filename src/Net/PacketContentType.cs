namespace RtmpSharp.Net
{
    enum PacketContentType : byte
    {
        SetChunkSize              = 1,
        AbortMessage              = 2,
        Acknowledgement           = 3,
        UserControlMessage        = 4,
        WindowAcknowledgementSize = 5,
        SetPeerBandwith           = 6,

        Audio                     = 8,
        Video                     = 9,
        
        DataAmf3                  = 15, // 0x0f | stream send
        SharedObjectAmf3          = 16, // 0x10 | shared obj
        CommandAmf3               = 17, // 0x11 | aka invoke

        DataAmf0                  = 18, // 0x12 | stream metadata
        SharedObjectAmf0          = 19, // 0x13 | shared object
        CommandAmf0               = 20, // 0x14 | aka invoke
        
        Aggregate                 = 22,
    }
}
