using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace RtmpSharp.IO.AMF3
{
    [Serializable]
    [TypeConverter(typeof(ArrayCollectionConverter))]
    [SerializedName("flex.messaging.io.ArrayCollection")]
    public class ArrayCollection : List<object>, IExternalizable
    {
        public void ReadExternal(IDataInput input)
        {
            var obj = input.ReadObject() as object[];
            if (obj != null)
                this.AddRange(obj);
        }

        public void WriteExternal(IDataOutput output)
        {
            output.WriteObject(this.ToArray());
        }
    }

    public class ArrayCollectionConverter : TypeConverter
    {
        static readonly Type[] ConvertibleTypes = new[]
        {
            typeof(ArrayCollection),
            typeof(System.Collections.IList)
        };

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return MiniTypeConverter.ConvertTo(value, destinationType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType.IsArray || ConvertibleTypes.Any(x => x == destinationType);

        }
    }
}
