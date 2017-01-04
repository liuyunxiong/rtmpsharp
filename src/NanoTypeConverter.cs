using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Hina;
using Hina.Linq;
using Konseki;

namespace RtmpSharp
{
    static class NanoTypeConverter
    {
        public static T ConvertTo<T>(object obj)
            => (T)ConvertTo(obj, typeof(T));

        public static object ConvertTo(object obj, Type target)
        {
            if (obj == null)
                return CreateDefaultValue(target);


            var source = obj.GetType();
            if (source == target || target.IsInstanceOfType(obj))
                return obj;


            var sourceInfo = source.GetTypeInfo();
            var targetInfo = source.GetTypeInfo();


            // IConvertible
            if (source.IsConvertible() && target.IsConvertible())
            {
                if (targetInfo.IsEnum)
                    return obj is string str ? Enum.Parse(target, str, ignoreCase: true) : Enum.ToObject(target, obj);

                return BclConvert(source, target, obj);
            }

            var ienumerable = obj as IEnumerable;


            // array
            if (target.IsArray && ienumerable != null)
            {
                var sourceElement = source.GetElementType();
                var targetElement = target.GetElementType();

                var enumerable = targetElement.IsAssignableFrom(sourceElement)
                    ? ienumerable.Cast<object>()
                    : ienumerable.Cast<object>().Select(x => ConvertTo(x, targetElement));

                return Helper.GetToArray(targetElement, enumerable);
            }


            // IDictionary<K, V>
            //     - we always deserialize AMF dictionaries as Dictionary<string, object> (or an object that inherits from it)
            if (obj is IDictionary<string, object> genericDictionary && GetGenericInterface(targetInfo, typeof(IDictionary<,>), out var dictionaryInterface))
            {
                var instance = MethodFactory.CreateInstance(target);
                var (add, keyType, valueType) = Helper.GetAddMethod(dictionaryInterface);

                foreach (var (key, value) in genericDictionary)
                    add(instance, (key, keyType), ConvertTo(value, valueType));

                return instance;
            }


            // IDictionary
            if (typeof(IDictionary).IsAssignableFrom(target) && obj is IDictionary dictionary)
            {
                var instance = (IDictionary)MethodFactory.CreateInstance(target);

                foreach (DictionaryEntry x in dictionary)
                    instance.Add(x.Key, x.Value);

                return instance;
            }

            // IList<T>
            if (ienumerable != null && GetGenericInterface(targetInfo, typeof(IList<>), out var listInterface))
            {
                var instance = MethodFactory.CreateInstance(target);
                var (add, type, _) = Helper.GetAddMethod(listInterface);

                foreach (var item in ienumerable)
                    add(instance, ConvertTo(item, type), null);

                return instance;
            }


            // IList
            if (typeof(IList).IsAssignableFrom(target) && ienumerable != null)
            {
                var instance = (IList)MethodFactory.CreateInstance(target);

                foreach (var item in ienumerable)
                    instance.Add(item);

                return instance;
            }


            // Guid
            if (target == typeof(Guid))
            {
                if (obj is string input)
                    return Guid.Parse(input);

                if (obj is byte[] bytes)
                    return new Guid(bytes);
            }


            // Nullable<T>
            if (targetInfo.IsNullable())
            {
                return Convert.ChangeType(
                    obj,
                    Nullable.GetUnderlyingType(target),
                    CultureInfo.InvariantCulture);
            }


            return BclConvert(source, target, obj);
        }

        // convert an object from type `a` to type `b` using underlying bcl conversion routines
        static object BclConvert(Type a, Type b, object value)
        {
            var convert = TypeDescriptor.GetConverter(a);

            return convert.CanConvertTo(b)
                ? convert.ConvertTo(null, CultureInfo.InvariantCulture, value, b)
                : Convert.ChangeType(value, b, CultureInfo.InvariantCulture);
        }

        static object CreateDefaultValue(Type type)
            => type.GetTypeInfo().IsValueType
                ? MethodFactory.CreateInstance(type)
                : null;

        // may return null
        static bool GetGenericInterface(TypeInfo type, Type generic, out Type @interface)
            => (@interface = type.GetInterfaces().FirstOrDefault(x => x.GetTypeInfo().IsGenericType && x.GetGenericTypeDefinition() == generic)) != null;
    }

    static class Helper
    {
        public static bool IsConvertible(this Type type)  => typeof(IConvertible).IsAssignableFrom(type);
        public static bool IsNullable(this TypeInfo type) => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);


        static readonly ConcurrentDictionary<Type, (Action<object, object, object>, Type, Type)> AddMethods = new ConcurrentDictionary<Type, (Action<object, object, object>, Type, Type)>();

        // returns a method:
        //     add(instance, key, value) for dictionaries
        //     add(instance, element, _) for collections
        public static (Action<object, object, object> invoke, Type key, Type value) GetAddMethod(Type type)
            => AddMethods.GetOrAdd(type, CreateAddMethod);

        static (Action<object, object, object>, Type, Type) CreateAddMethod(Type type)
        {
            var method = type.GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
            var args   = type.GetGenericArguments();

            if (args.Length == 1)
            {
                // add(item), usually to add into a collection
                var instance      = Expression.Parameter(typeof(object));
                var element       = Expression.Parameter(typeof(object));
                var dummy         = Expression.Parameter(typeof(object));
                var typedInstance = Expression.Convert(instance, type);
                var typedElement  = Expression.Convert(element, args[0]);
                var expression    = Expression.Call(typedInstance, method, typedElement);
                var lambda        = Expression.Lambda<Action<object, object, object>>(expression, instance, element, dummy).Compile();

                return (lambda, args[0], null);
            }
            if (args.Length == 2)
            {
                // add(key, value), usually to add into a dictionary
                var instance      = Expression.Parameter(type);
                var key           = Expression.Parameter(args[0]);
                var value         = Expression.Parameter(args[1]);
                var typedInstance = Expression.Convert(instance, type);
                var typedKey      = Expression.Convert(key, args[0]);
                var typedValue    = Expression.Convert(value, args[0]);
                var expression    = Expression.Call(typedInstance, method, typedKey, typedValue);
                var lambda        = Expression.Lambda<Action<object, object, object>>(expression).Compile();

                return (lambda, args[0], args[1]);
            }
            else
            {
                Kon.DebugWarn("interface doesn't have an add method in the form 'Add(key, value)'", new { type, arguments = args.Length });

                return ((_1, _2, _3) => { }, null, null);
            }
        }


        static readonly ConcurrentDictionary<Type, Func<IEnumerable<object>, object>> ToArray = new ConcurrentDictionary<Type, Func<IEnumerable<object>, object>>();

        public static object GetToArray(Type element, IEnumerable<object> enumerable)
            => ToArray.GetOrAdd(element, CreateToArray)(enumerable);

        static Expression<Func<IEnumerable<object>, object>> ToArrayInternal<T>()
            => e => e.MapArray(x => (T)x);

        static Func<IEnumerable<object>, object> CreateToArray(Type elementType)
        {
            var generic    = typeof(Helper).GetMethod("ToArrayInternal", BindingFlags.Static | BindingFlags.NonPublic);
            var method     = generic.MakeGenericMethod(elementType);
            var expression = (Expression<Func<IEnumerable<object>, object>>)method.Invoke(null, null);

            return expression.Compile();
        }
    }
}
