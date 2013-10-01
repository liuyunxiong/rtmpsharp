using System.ComponentModel;

namespace RtmpSharp
{
    public static class TypeSerializer
    {
        public static void RegisterTypeConverters()
        {
            TypeDescriptor.AddAttributes(typeof(string), new TypeConverterAttribute(typeof(RtmpSharp.IO.TypeConverters.StringConverter)));
        }
    }
}
