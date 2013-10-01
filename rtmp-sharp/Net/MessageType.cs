
namespace RtmpSharp.Net
{
    enum MessageType : byte
    {
        SetChunkSize = 1,
        AbortMessage = 2,
        Acknowledgement = 3,
        UserControlMessage = 4,
        WindowAcknowledgementSize = 5,
        SetPeerBandwith = 6,

        Audio = 8,
        Video = 9,
        
        DataAmf3 = 15, // stream send, 0x0f
        SharedObjectAmf3 = 16, // shared obj, 0x10
        CommandAmf3 = 17, // aka invoke, 0x11

        DataAmf0 = 18, // stream metadata, 0x12
        SharedObjectAmf0 = 19, // 0x13
        CommandAmf0 = 20, // aka invokex 0x14
        
        Aggregate = 22,
    }
}
