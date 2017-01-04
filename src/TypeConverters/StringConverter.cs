using System;
using System.ComponentModel;
using System.Globalization;

namespace RtmpSharp.TypeConverters
{
    // converts a single-character `string` into a `char`
    class StringToCharConverter : StringConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(char)
                || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            => value is char
                ? value.ToString()
                : base.ConvertFrom(context, culture, value);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string)
                || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            if (value is string str)
            {
                switch (str.Length)
                {
                    case 0:  return null;
                    case 1:  return str[0];
                    default: throw new ArgumentException("cannot convert a multi-character string into a single character");
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
