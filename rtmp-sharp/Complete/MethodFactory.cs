using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

// astralfoxy:complete/methodfactory.cs
namespace Complete
{
    // thread-safe.
    static class MethodFactory
    {
        public delegate object ConstructorCall(object[] args);
        public delegate object MethodCall(object instance, object[] args);

        static readonly ConcurrentDictionary<object, ConstructorCall> ConstructorCache = new ConcurrentDictionary<object, ConstructorCall>();
        static readonly ConcurrentDictionary<object, MethodCall> MethodCache = new ConcurrentDictionary<object, MethodCall>();

        struct ExpressionArgsPair
        {
            public Expression Expression;
            public ParameterExpression[] Parameters;
        }
        static ExpressionArgsPair GetObjectCreatorExpressions(ConstructorInfo constructor)
        {
            // create a single param of type object[]
            var parameters = Expression.Parameter(typeof(object[]), "args");
            var arguments = GetMethodCallArgsExpressions(constructor.GetParameters(), parameters);

            // call method with args
            var caller = Expression.New(constructor, arguments);

            return new ExpressionArgsPair()
            {
                Expression = caller,
                Parameters = new[] { parameters }
            };
        }





        public static ConstructorCall CompileObjectConstructor(ConstructorInfo constructor)
        {
            var expressions = GetObjectCreatorExpressions(constructor);
            var lambda = Expression.Lambda<ConstructorCall>(expressions.Expression, expressions.Parameters);
            return lambda.Compile();
        }

        // does the same as `CompileObjectConstructor`, but with an extra boxing expression
        public static ConstructorCall CompileBoxedObjectConstructor(ConstructorInfo constructor)
        {
            var expressions = GetObjectCreatorExpressions(constructor);
            var conversion = Expression.Convert(expressions.Expression, typeof(object));
            var lambda = Expression.Lambda<ConstructorCall>(conversion, expressions.Parameters);
            return lambda.Compile();
        }

        public static T CreateInstance<T>(Type type)
        {
            return (T)CreateInstance(type);
        }
        public static object CreateInstance(Type type)
        {
            var creator = ConstructorCache.GetOrAdd(type, (object _) =>
            {
                var constructor = type.GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 0);
                if (constructor == default(ConstructorInfo))
                    throw new ArgumentException("Type does not have any accessible parameterless constructors.");
                return CompileObjectConstructor(constructor);
            });
            return creator(new object[0]);
        }









        static ExpressionArgsPair GetMethodCallExpressions(MethodInfo method)
        {
            // create a single param of type object[]
            var instanceParameter = Expression.Parameter(typeof(object), "instance");
            var instance = Expression.Convert(instanceParameter, method.DeclaringType);

            var parameters = Expression.Parameter(typeof(object[]), "args");
            var arguments = GetMethodCallArgsExpressions(method.GetParameters(), parameters);

            // call method with args
            var callInstance = method.IsStatic ? null : instance;
            var caller = (Expression)Expression.Call(callInstance, method, arguments);

            // the lambda compilation won't work without this - `void` cannot be coerced into an `object`
            if (method.ReturnType == typeof(void))
                caller = Expression.Block(caller, Expression.Default(typeof(object)));

            return new ExpressionArgsPair()
            {
                Expression = caller,
                Parameters = new[] { instanceParameter, parameters }
            };
        }

        public static MethodCall CompileMethodCall(MethodInfo method)
        {
            var expressions = GetMethodCallExpressions(method);
            var lambda = Expression.Lambda<MethodCall>(expressions.Expression, expressions.Parameters);
            return lambda.Compile();
        }

        // does the same as `CompileObjectConstructor`, but with an extra boxing expression
        public static MethodCall CompileBoxedMethodCall(MethodInfo method)
        {
            var expressions = GetMethodCallExpressions(method);
            var conversion = Expression.Convert(expressions.Expression, typeof(object));
            var lambda = Expression.Lambda<MethodCall>(conversion, expressions.Parameters);
            return lambda.Compile();
        }

        public static T CallMethod<T>(MethodInfo method, object instance, object[] arguments)
        {
            return (T)CallMethod(method, instance, arguments);
        }
        public static object CallMethod(MethodInfo method, object instance, object[] arguments)
        {
            var call = MethodCache.GetOrAdd(method, _ => CompileMethodCall(method));
            return call(instance, arguments);
        }









        private static Expression[] GetMethodCallArgsExpressions(ParameterInfo[] parameters, ParameterExpression args)
        {
            return Enumerable.Range(0, parameters.Length).Select(i =>
            {
                // create a type cast expression for each argument
                var index = Expression.Constant(i);
                var parameterType = parameters[i].ParameterType;
                var parameterAccess = Expression.ArrayIndex(args, index);
                return (Expression)Expression.Convert(parameterAccess, parameterType);
            }).ToArray();
        }
    }
}
