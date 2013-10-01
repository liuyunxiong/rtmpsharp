using System;
using System.ComponentModel;
using System.Globalization;

namespace RtmpSharp.IO.TypeConverters
{
    // Adds support for converter converting a string to a char
    class StringConverter : System.ComponentModel.StringConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(char))
                return true;
            return base.CanConvertFrom(context, sourceType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            if (value is char)
                return value.ToString();
            return base.ConvertFrom(context, culture, value);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            var str = value as string;
            if (str != null)
            {
                if (str.Length == 0)
                    return null;
                if (str.Length == 1)
                    return str[0];
                throw new ArgumentException("Cannot convert string to char: string length is too long.");
            }
            return base.ConvertTo(context, culture, value, destinationType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            if (destinationType == typeof(string))
                return true;
            return base.CanConvertTo(context, destinationType);
        }
    }
}
