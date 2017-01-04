using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace RtmpSharp.IO.AMF3
{
    [TypeConverter(typeof(ArrayCollectionConverter))]
    [RtmpSharp("flex.messaging.io.ArrayCollection")]
    public class ArrayCollection : List<object>, IExternalizable
    {
        public void ReadExternal(IDataInput input)
        {
            if (input.ReadObject() is object[] obj)
                AddRange(obj);
        }

        public void WriteExternal(IDataOutput output)
        {
            output.WriteObject(ToArray());
        }
    }

    public class ArrayCollectionConverter : TypeConverter
    {
        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
            => NanoTypeConverter.ConvertTo(value, destinationType);

        public override bool CanConvertTo(ITypeDescriptorContext context, Type type)
            => type.IsArray
            || type == typeof(ArrayCollection)
            || type == typeof(IList)
            || (type.GetTypeInfo().IsGenericType && type.GetGenericTypeDefinition() == typeof(List<>));
    }
}
