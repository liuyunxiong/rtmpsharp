using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Hina;
using Hina.Collections;
using RtmpSharp.IO.AMF3;
using RtmpSharp.Messaging.Messages;

namespace RtmpSharp
{
    class TypeRegistry
    {
        readonly Dictionary<Type, MethodFactory.ConstructorCall> constructors = new Dictionary<Type, MethodFactory.ConstructorCall>();
        readonly Dictionary<string, Type>   localTypeLookup  = new Dictionary<string, Type>();
        readonly Dictionary<string, string> remoteNameLookup = new Dictionary<string, string>();


        public TypeRegistry()
        {
            foreach (var type in DefaultTypes)
                RegisterType(type);
        }


        public string CanonicalName(string name) => remoteNameLookup.GetDefault(name, name);
        public bool   Exists(string name)        => localTypeLookup.TryGetValue(name, out var type) && constructors.ContainsKey(type);
        public object CreateOrNull(string name)  => localTypeLookup.TryGetValue(name, out var type) ? constructors[type](EmptyCollection<object>.Array) : null;


        // registry this type with the registry
        public void RegisterType(Type type)
        {
            var info = type.GetTypeInfo();
            if (info.IsEnum || constructors.ContainsKey(type))
                return;

            var constructor = type.GetConstructors().FirstOrDefault(x => x.GetParameters().Length == 0);
            if (constructor == null)
                throw new ArgumentException($"{type.FullName} does not have any accessible parameterless constructors.", nameof(type));

            var attribute     = info.GetCustomAttribute<RtmpSharpAttribute>(false) ?? RtmpSharpAttribute.Empty;
            var canonicalName = attribute.CanonicalName ?? type.FullName;
            var names         = attribute.Names ?? EmptyCollection<string>.Array;

            foreach (var name in names)
                localTypeLookup[name] = type;

            constructors[type] = MethodFactory.CompileConstructor(constructor);
            remoteNameLookup[type.FullName] = canonicalName;

            if (canonicalName != "")
                localTypeLookup[canonicalName] = type;
        }


        static readonly Type[] DefaultTypes =
        {
            typeof(AcknowledgeMessage),
            typeof(ArrayCollection),
            typeof(AsyncMessage),
            typeof(ByteArray),
            typeof(CommandMessage),
            typeof(ErrorMessage),
            typeof(FlexMessage),
            typeof(ObjectProxy),
            typeof(RemotingMessage)
        };
    }
}
