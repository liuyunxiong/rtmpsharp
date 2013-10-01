using Complete;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Reflection;

namespace RtmpSharp.IO
{
    static class MiniTypeConverter
    {
        static Func<object, object> Passthrough = x => x;

        static MethodInfo EnumerableToArrayMethod = typeof(MiniTypeConverter).GetMethod("EnumerableToArray", BindingFlags.Static | BindingFlags.NonPublic);
        static T[] EnumerableToArray<T>(IEnumerable enumerable) { return enumerable.Cast<T>().ToArray(); }

        static ConcurrentDictionary<Type, MethodInfo> EnumerableToArrayCache = new ConcurrentDictionary<Type, MethodInfo>();
        static ConcurrentDictionary<Type, AdderMethodInfo> AdderMethodCache = new ConcurrentDictionary<Type, AdderMethodInfo>();

        static MiniTypeConverter()
        {
        }

        public static object ConvertTo(object value, Type targetType)
        {
            if (value == null)
                return CreateDefaultValue(targetType);

            var sourceType = value.GetType();
            if (sourceType == targetType || targetType.IsInstanceOfType(value))
                return value;

            // IConvertible
            if (Reflection.IsConvertible(sourceType) && Reflection.IsConvertible(targetType))
            {
                if (targetType.IsEnum)
                {
                    var stringValue = value as string;
                    if (stringValue != null)
                        return Enum.Parse(targetType, stringValue, true);
                    
                    return Enum.ToObject(targetType, value);
                }

                return ConvertObject(sourceType, targetType, value);
            }

            var ienumerable = value as IEnumerable;

            // Array
            if (targetType.IsArray && ienumerable != null)
            {
                var sourceElementType = sourceType.GetElementType();
                var destinationElementType = targetType.GetElementType();

                var enumerable = ienumerable.Cast<object>();

                if (!destinationElementType.IsAssignableFrom(sourceElementType))
                    enumerable = enumerable.Select(x => ConvertTo(x, destinationElementType));

                var method = EnumerableToArrayCache.GetOrAdd(destinationElementType, type => EnumerableToArrayMethod.MakeGenericMethod(new[] { type }));
                return method.Invoke(null, new object[] { enumerable });
            }

            // IDictionary<K, V>
            //     - We always deserialize AMF dictionaries as Dictionary<string, object> (or an object that inherits from it)
            var sourceGenericDictionary = value as IDictionary<string, object>;
            var genericDictionaryType = TryGetInterfaceType(targetType, typeof(IDictionary<,>));
            if (sourceGenericDictionary != null && genericDictionaryType != null)
            {
                var instance = MethodFactory.CreateInstance(targetType);
                var adder = AdderMethodCache.GetOrAdd(genericDictionaryType, type => new AdderMethodInfo(type));

                foreach (var pair in sourceGenericDictionary)
                {
                    adder.Method.Invoke(instance, new[]
                    {
                        ConvertTo(pair.Key, adder.TypeGenericParameters[0]),
                        ConvertTo(pair.Value, adder.TypeGenericParameters[1])
                    });
                }

                return instance;
            }

            // IDictionary
            var souceDictionary = value as IDictionary;
            if (typeof(IDictionary).IsAssignableFrom(targetType) && souceDictionary != null)
            {
                var instance = (IDictionary)MethodFactory.CreateInstance(targetType);
                foreach (DictionaryEntry pair in souceDictionary)
                    instance.Add(pair.Key, pair.Value);
                return instance;
            }

            // IList<T>
            var sourceListType = TryGetInterfaceType(targetType, typeof(IList<>));
            if (sourceListType != null && ienumerable != null)
            {
                var instance = MethodFactory.CreateInstance(targetType);
                var adder = AdderMethodCache.GetOrAdd(sourceListType, type => new AdderMethodInfo(type));

                foreach (var item in ienumerable)
                    adder.Method.Invoke(instance, new object[] { ConvertTo(item, adder.TypeGenericParameters[0]) });
                return instance;
            }


            // IList
            if (typeof(IList).IsAssignableFrom(targetType) && ienumerable != null)
            {
                var instance = (IList)MethodFactory.CreateInstance(targetType);
                foreach (var item in ienumerable)
                    instance.Add(item);
                return instance;
            }

            // Guid
            if (targetType == typeof(Guid))
            {
                var input = value as string;
                if (input != null)
                    return Guid.Parse(input);

                var bytes = value as byte[];
                if (bytes != null)
                    return new Guid(bytes);
            }

            // Nullable<T>
            if (targetType.IsNullable())
            {
                var underlyingType = Nullable.GetUnderlyingType(targetType);
                return Convert.ChangeType(value, underlyingType, CultureInfo.InvariantCulture);
            }

            return ConvertObject(sourceType, targetType, value);
        }

        static object ConvertObject(Type sourceType, Type targetType, object value)
        {
            var converter = TypeDescriptor.GetConverter(sourceType);
            if (converter.CanConvertTo(targetType))
                return converter.ConvertTo(null, CultureInfo.InvariantCulture, value, targetType);

            return Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
        }

        static object CreateDefaultValue(Type type)
        {
            if (type.IsValueType)
                return Activator.CreateInstance(type);
            return null;
        }

        static Type TryGetInterfaceType(Type targetType, Type type)
        {
            return targetType.GetInterfaces()
                .Where(x => x.IsGenericType)
                .FirstOrDefault(x => typeof(IDictionary<,>) == x.GetGenericTypeDefinition());
        }

        struct AdderMethodInfo
        {
            public MethodInfo Method;
            public Type[] TypeGenericParameters;

            public AdderMethodInfo(Type genericType)
            {
                Method = genericType.GetMethod("Add");
                TypeGenericParameters = genericType.GetGenericArguments();
            }
        }

    }

    static class Reflection
    {
        public static bool IsNullable(this Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        public static bool IsConvertible(this Type type)
        {
            return typeof(IConvertible).IsAssignableFrom(type);
        }
    }
}
