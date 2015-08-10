using RtmpSharp.IO.AMF3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RtmpSharp.IO.ObjectWrappers
{
    class BasicObjectWrapper : IObjectWrapper
    {
        // todo: make this a real cache... which expires old items.
        readonly Dictionary<Type, ClassDescription> cache = new Dictionary<Type, ClassDescription>();

        readonly SerializationContext context;

        public bool GetIsExternalizable(object instance) => instance is IExternalizable;
        public bool GetIsDynamic(object instance) => instance is AsObject;

        public BasicObjectWrapper(SerializationContext context)
        {
            this.context = context;
        }

        // gets the class definition for an object `obj`, applying transformations like type name mappings
        public virtual ClassDescription GetClassDescription(object obj)
        {
            if (obj == null)
                throw new ArgumentNullException(nameof(obj));

            var type = obj.GetType();

            ClassDescription cachedDescription;
            if (cache.TryGetValue(type, out cachedDescription))
                return cachedDescription;

            var serializable = Helper.GetSerializableFields(type);
            var classMembers = new List<BasicMemberWrapper>();

            // add fields and properties
            classMembers.AddRange(serializable.Fields.Select(fieldInfo => new BasicMemberWrapper(fieldInfo)));
            foreach (var propertyInfo in serializable.Properties)
            {
                // There is no reflection API that allows us to check whether a variable hides
                // another variable (for example, with the `new` keyword). We need to access the
                // property by name and catch an ambiguous match.
                //
                // Currently, the logic following that which only allows variables from the
                // declaring type forward doesn't work in all cases work in all cases. Assume we
                // have an inheritence hierarchy of `operating-system -> linux -> arch-linux`. In
                // this case, if both `operating-system` and `linux` declare a `Name` field that is
                // not inherited but `arch-linux` does not then we expect the `Name` field from
                // `linux` to be serialized, but as it is both `Name` fields from `operating-system`
                // and `linux` are ignored.
                //
                //
                // In practice it does not matter for my current use cases, but this may trip up
                // some people if I ever open source this
                try
                {
                    type.GetProperty(propertyInfo.Name);
                }
                catch (AmbiguousMatchException)
                {
                    if (type.DeclaringType != type)
                        continue;
                }

                classMembers.Add(new BasicMemberWrapper(propertyInfo));
            }

            return new BasicObjectClassDescription(
                context.GetAlias(type.FullName),
                classMembers.Cast<IMemberWrapper>().ToArray(),
                GetIsExternalizable(obj),
                GetIsDynamic(obj));
        }

        class BasicObjectClassDescription : ClassDescription
        {
            // because we are cached by the `BasicObjectWrapper`, speed up lookups so that read deserialisation is (slightly) faster.
            Dictionary<string, IMemberWrapper> MemberLookup { get; }

            internal BasicObjectClassDescription(string name, IMemberWrapper[] members, bool externalizable, bool dynamic)
                : base(name, members, externalizable, dynamic)
            {
                this.MemberLookup = members
                    .Select(x => x.SerializedName == null ? new { Name = x.Name, Member = x } : new { Name = x.SerializedName, Member = x })
                    .ToLookup(x => x.Name)
                    .ToDictionary(x => x.Key, x => x.First().Member);
            }

            public override bool TryGetMember(string name, out IMemberWrapper member)
            {
                return MemberLookup.TryGetValue(name, out member);
            }
        }

        class BasicMemberWrapper : IMemberWrapper
        {
            readonly bool isField;
            readonly PropertyInfo propertyInfo;
            readonly FieldInfo fieldInfo;

            public string Name { get; }
            public string SerializedName { get; }

            public BasicMemberWrapper(PropertyInfo propertyInfo)
            {
                this.propertyInfo = propertyInfo;
                this.isField = false;

                this.Name = propertyInfo.Name;
                this.SerializedName = propertyInfo.GetCustomAttribute<SerializedNameAttribute>(true)?.SerializedName ?? Name;
            }

            public BasicMemberWrapper(FieldInfo fieldInfo)
            {
                this.fieldInfo = fieldInfo;
                this.isField = true;

                this.Name = fieldInfo.Name;
                this.SerializedName = fieldInfo.GetCustomAttribute<SerializedNameAttribute>(true)?.SerializedName ?? Name;
            }

            public object GetValue(object instance)
            {
                return isField ? fieldInfo.GetValue(instance) : propertyInfo.GetValue(instance);
            }

            public void SetValue(object instance, object value)
            {
                var target = isField ? fieldInfo.FieldType : propertyInfo.PropertyType;
                var converted = MiniTypeConverter.ConvertTo(value, target);

                if (isField)
                    fieldInfo.SetValue(instance, converted);
                else
                    propertyInfo.SetValue(instance, converted);
            }
        }

        static class Helper
        {
            public struct FieldsAndProperties
            {
                public PropertyInfo[] Properties;
                public FieldInfo[] Fields;
            }

            public static FieldsAndProperties GetSerializableFields(object obj)
            {
                return GetSerializableFields(obj.GetType());
            }

            public static FieldsAndProperties GetSerializableFields(Type type)
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.GetCustomAttributes(typeof(TransientAttribute), true).Length == 0)
                    .Where(x => x.GetGetMethod()?.GetParameters().Length == 0); // skip if not a "pure" get property, aka has parameters (eg `class[int index]`)

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.GetCustomAttributes(typeof(NonSerializedAttribute), true).Length == 0)
                    .Where(x => x.GetCustomAttributes(typeof(TransientAttribute), true).Length == 0);

                return new FieldsAndProperties()
                {
                    Properties = properties.ToArray(),
                    Fields = fields.ToArray()
                };
            }
        }
    }
}