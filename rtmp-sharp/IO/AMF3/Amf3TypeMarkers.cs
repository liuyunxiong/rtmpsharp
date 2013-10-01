
namespace RtmpSharp.IO.AMF3
{
    enum Amf3TypeMarkers : byte
    {
        Undefined = 0x00,      // 0x00 | 0
        Null = 0x01,           // 0x01 | 1
        False = 0x02,          // 0x02 | 2
        True = 0x03,           // 0x03 | 3
        Integer = 0x04,        // 0x04 | 4
        Double = 0x05,         // 0x05 | 5
        String = 0x06,         // 0x06 | 6
        LegacyXml = 0x07,      // 0x07 | 7
        Date = 0x08,           // 0x08 | 8
        Array = 0x09,          // 0x09 | 9
        Object = 0x0A,         // 0x0A | 10
        Xml = 0x0B,            // 0x0B | 11
        ByteArray = 0x0C,      // 0x0C | 12
        VectorInt = 0x0D,      // 0x0D | 13
        VectorUInt = 0x0E,     // 0x0E | 14
        VectorDouble = 0x0F,   // 0x0F | 15
        VectorObject = 0x10,   // 0x10 | 16
        Dictionary = 0x11      // 0x11 | 17
    };
}
