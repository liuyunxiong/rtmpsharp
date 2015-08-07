using System;
using System.ComponentModel;
using System.Globalization;

namespace RtmpSharp.IO.TypeConverters
{
    // adds support for converting a string to a char
    class StringConverter : System.ComponentModel.StringConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            => sourceType == typeof(char) || base.CanConvertFrom(context, sourceType);

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            => value is char ? value.ToString() : base.ConvertFrom(context, culture, value);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
            => destinationType == typeof(string) || base.CanConvertTo(context, destinationType);

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            var str = value as string;
            if (str != null)
            {
                switch (str.Length)
                {
                    case 0:
                        return null;
                    case 1:
                        return str[0];
                    default:
                        throw new ArgumentException("can't convert string to char - too long");
                }
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }
}
