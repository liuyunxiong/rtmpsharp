
namespace RtmpSharp.IO.AMF0
{
    enum Amf0TypeMarkers : byte
    {
        Number = 0x00,         // 0x00 | 0
        Boolean = 0x01,        // 0x01 | 1
        String = 0x02,         // 0x02 | 2
        Object = 0x03,         // 0x03 | 3
        Movieclip = 0x04,      // 0x04 | 4
        Null = 0x05,           // 0x05 | 5
        Undefined = 0x06,      // 0x06 | 6
        Reference = 0x07,      // 0x07 | 7
        EcmaArray = 0x08,      // 0x08 | 8
        ObjectEnd = 0x09,      // 0x09 | 9
        StrictArray = 0x0A,    // 0x0A | 10
        Date = 0x0B,           // 0x0B | 11
        LongString = 0x0C,     // 0x0C | 12
        Unsupported = 0x0D,    // 0x0D | 13
        Recordset = 0x0E,      // 0x0E | 14
        Xml = 0x0F,            // 0x0F | 15
        TypedObject = 0x10,    // 0x10 | 16
        Amf3Object = 0x11,     // 0x11 | 17
    };

}
