using System;
using System.Collections.Generic;
using Hina;

namespace RtmpSharp.IO.AMF3
{
    [RtmpSharp("flex.messaging.io.ObjectProxy")]
    class ObjectProxy : Dictionary<string, object>, IExternalizable
    {
        public void ReadExternal(IDataInput input)
        {
            if (input.ReadObject() is IDictionary<string, object> values)
            {
                foreach (var (key, value) in values)
                    this[key] = value;
            }
        }

        public void WriteExternal(IDataOutput output)
        {
            output.WriteObject(new AsObject(this, owned: true) { TypeName = "flex.messaging.io.ObjectProxy" });
        }
    }
}
