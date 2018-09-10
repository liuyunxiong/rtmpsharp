using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Hina;
using Hina.Linq;
using RtmpSharp.IO.AMF3;

namespace RtmpSharp.Infos
{
    class BasicObjectInfo : IObjectInfo
    {
        readonly ConcurrentDictionary<Type, ClassInfo> cache = new ConcurrentDictionary<Type, ClassInfo>();

        readonly SerializationContext context;

        public BasicObjectInfo(SerializationContext context) => this.context = context;
        public bool IsExternalizable(object instance)        => instance is IExternalizable;
        public bool IsDynamic(object instance)               => instance is AsObject;

        public virtual ClassInfo GetClassInfo(object instance)
        {
            Check.NotNull(instance);

            return cache.GetOrAdd(instance.GetType(), type =>
            {
                var (fields, properties) = Helper.GetSerializableFields(type);
                var members              = new List<IMemberInfo>();

                members.AddRange(
                    fields.Select(x => new BasicObjectMemberInfo(x)));

                foreach (var property in properties)
                {
                    // there is no reflection api that allows us to check whether a variable hides another variable (in c#,
                    // that would be with the `new` keyword). to do this, we have to manually attempt to access a property
                    // by name detect ambiguous matches.
                    try
                    {
                        type.GetProperty(property.Name);
                    }
                    catch (AmbiguousMatchException)
                    {
                        if (type.DeclaringType != type)
                            continue;
                    }

                    members.Add(new BasicObjectMemberInfo(property));
                }

                return new BasicObjectClassInfo(
                    name:           context.GetCanonicalName(type.FullName),
                    members:        members.ToArray(),
                    externalizable: IsExternalizable(instance),
                    dynamic:        IsDynamic(instance));
            });
        }

        class BasicObjectClassInfo : ClassInfo
        {
            IDictionary<string, IMemberInfo> lookup;

            public BasicObjectClassInfo(string name, IMemberInfo[] members, bool externalizable, bool dynamic) : base(name, members, externalizable, dynamic)
                => lookup = members.ToQuickDictionary(
                    x => string.IsNullOrEmpty(x.Name) ? x.LocalName : x.Name,
                    x => x);

            public override bool TryGetMember(string name, out IMemberInfo member)
                => lookup.TryGetValue(name, out member);
        }

        class BasicObjectMemberInfo : IMemberInfo
        {
            readonly Func<object, object>   getValue;
            readonly Action<object, object> setValue;
            readonly Type                   valueType;

            public string LocalName { get; }
            public string Name { get; }

            public BasicObjectMemberInfo(PropertyInfo property)
            {
                LocalName = property.Name;
                Name      = property.GetCustomAttribute<RtmpSharpAttribute>(true)?.CanonicalName ?? LocalName;

                getValue  = Helper.AccessProperty(property);
                setValue  = Helper.AssignProperty(property);
                valueType = property.PropertyType;
            }

            public BasicObjectMemberInfo(FieldInfo field)
            {
                LocalName = field.Name;
                Name      = field.GetCustomAttribute<RtmpSharpAttribute>(true)?.CanonicalName ?? LocalName;

                getValue  = Helper.AccessField(field);
                setValue  = Helper.AssignField(field);
                valueType = field.FieldType;
            }

            public object GetValue(object instance) => getValue(instance);
            public void   SetValue(object instance, object value) => setValue(instance, NanoTypeConverter.ConvertTo(value, valueType));
        }

        static class Helper
        {
            public static (FieldInfo[] fields, PropertyInfo[] properties) GetSerializableFields(object obj)
                => GetSerializableFields(obj.GetType());

            public static (FieldInfo[] fields, PropertyInfo[] properties) GetSerializableFields(Type type)
            {
                var properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.GetCustomAttributes(typeof(RtmpIgnoreAttribute), true).None())
                    .Where(x => x.GetGetMethod()?.GetParameters().Length == 0) // skip if not a "pure" get property, aka has parameters (eg `class[int index]`)
                    .ToArray();

                var fields = type.GetFields(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.GetCustomAttributes(typeof(RtmpIgnoreAttribute), true).None())
                    .ToArray();

                return (fields, properties);
            }

            public static Action<object, object> AssignField(FieldInfo field)
            {
                var instance = Expression.Parameter(typeof(object));
                var value    = Expression.Parameter(typeof(object));
                var assign   = Expression.Assign(
                    Expression.Field(
                        Expression.Convert(instance, field.DeclaringType),
                        field),
                    Expression.Convert(value, field.FieldType));

                return Expression.Lambda<Action<object, object>>(assign, instance, value).Compile();
            }

            public static Func<object, object> AccessField(FieldInfo field)
            {
                var instance = Expression.Parameter(typeof(object));
                var access   = Expression.Convert(
                    Expression.Field(
                        Expression.Convert(instance, field.DeclaringType),
                        field),
                    typeof(object));

                return Expression.Lambda<Func<object, object>>(access, instance).Compile();
            }

            public static Action<object, object> AssignProperty(PropertyInfo property)
            {
                var instance = Expression.Parameter(typeof(object));
                var value    = Expression.Parameter(typeof(object));
                var assign   = Expression.Assign(
                    Expression.Property(
                        Expression.Convert(instance, property.DeclaringType),
                        property),
                    Expression.Convert(value, property.PropertyType));

                return Expression.Lambda<Action<object, object>>(assign, instance, value).Compile();
            }

            public static Func<object, object> AccessProperty(PropertyInfo property)
            {
                var instance = Expression.Parameter(typeof(object));
                var access   = Expression.Convert(
                    Expression.Property(
                        Expression.Convert(instance, property.DeclaringType),
                        property),
                    typeof(object));

                return Expression.Lambda<Func<object, object>>(access, instance).Compile();
            }
        }
    }
}