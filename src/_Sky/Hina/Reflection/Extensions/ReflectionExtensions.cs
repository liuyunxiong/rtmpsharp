using System;
using System.Reflection;

// csharp: hina/reflection/reflectionextensions.cs [snipped]
namespace Hina.Reflection
{
    static class ReflectionExtensions
    {
        public static bool ImplementsGenericInterface(this TypeInfo type, Type target)
        {
            foreach (var @interface in type.ImplementedInterfaces)
            {
                var info = @interface.GetTypeInfo();

                if (info.IsGenericType && info.GetGenericTypeDefinition() == target)
                    return true;
            }

            return false;
        }

        public static bool ImplementsGenericInterface(this Type type, Type target)
            => ImplementsGenericInterface(type.GetTypeInfo(), target);
    }
}
