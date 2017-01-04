using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hina;

// csharp: konseki/kon/objectdumper.static-facade.cs [snipped]
namespace Konseki
{
    // dumps an object's fields to a simple text format that we use for basic text logging in debug mode
    static class ObjectDumper
    {
        static readonly string[] Empty = { };

        public static IEnumerable<string> GetLines(object obj)
        {
            return obj == null ? Empty : GetProperties(obj).Select(x => $"{x.name}: {x.value}");
        }

        public static string GetText(object obj, string indent = "    ")
        {
            return string.Join("\n", GetLines(obj).Select(x => $"{indent}{x}"));
        }

        static IEnumerable<(string name, string value)> GetProperties(object obj)
        {
            var (fields, properties) = TypeCache.Get(obj.GetType());

            foreach (var x in fields)
                yield return (name: x.Name.Camelize(), value: x.GetValue(obj)?.ToString() ?? "[null]");

            foreach (var x in properties)
                yield return (name: x.Name.Camelize(), value: x.GetValue(obj)?.ToString() ?? "[null]");
        }


        static class TypeCache
        {
            static readonly ConcurrentDictionary<Type, (FieldInfo[], PropertyInfo[])> Cache = new ConcurrentDictionary<Type, (FieldInfo[], PropertyInfo[])>();

            public static (FieldInfo[], PropertyInfo[]) Get(Type type)
            {
                const BindingFlags Flags = BindingFlags.Public | BindingFlags.Instance;

                return Cache.GetOrAdd(type, x => (x.GetFields(Flags), x.GetProperties(Flags)));
            }
        }
    }
}
