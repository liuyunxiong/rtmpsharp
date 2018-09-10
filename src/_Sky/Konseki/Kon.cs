using System;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;

// csharp: konseki/kon.static-facade.cs [snipped]
namespace Konseki
{
    static class Kon
    {
        [Conditional("DEBUG"), DebuggerStepThrough] public static void Assert(bool condition, string message) => System.Diagnostics.Debug.Assert(condition, message);
        [Conditional("DEBUG"), DebuggerStepThrough] public static void Assert(bool condition)                 => System.Diagnostics.Debug.Assert(condition);

        [Conditional("DEBUG")] public static void DebugRun(Action action)         => action();
        [Conditional("DEBUG")] public static void DebugRun(Action<object> action) => action(null);

        [Conditional("DEBUG")] public static void Trace        (string message, object data = null) => Log("trace",    message, data);
        [Conditional("DEBUG")] public static void Debug        (string message, object data = null) => Log("debug",    message, data);
        [Conditional("DEBUG")] public static void DebugEmit    (string message, object data = null) => Log("info",     message, data);
        [Conditional("DEBUG")] public static void DebugWarn    (string message, object data = null) => Log("warning",  message, data);
        [Conditional("DEBUG")] public static void DebugError   (string message, object data = null) => Log("error",    message, data);
        [Conditional("DEBUG")] public static void DebugCritical(string message, object data = null) => Log("critical", message, data);

        public static void Emit    (string message, object data = null) => Log("info",     message, data);
        public static void Warn    (string message, object data = null) => Log("warning",  message, data);
        public static void Error   (string message, object data = null) => Log("error",    message, data);
        public static void Critical(string message, object data = null) => Log("critical", message, data);

        static void Log(string level, string message, object data = null)
            => Log(level, NameResolver.GetName(), message, data);

        static void Log(string level, string name, string message, object data = null)
        {
            var dataText = data != null
                ? "\n" + ObjectDumper.GetText(data)
                : "";

            Console.WriteLine($"[{DateTime.Now :u}] {level}: {name}: {message}{dataText}", level, name, message, dataText);
        }


        [Conditional("DEBUG")] public static void DebugException(Exception exception, object data = null, [CallerMemberName] string callerName = "", [CallerFilePath] string fileName = "")                { DebugException(null, exception, data, callerName, fileName); }
        [Conditional("DEBUG")] public static void DebugException(string prefix, Exception exception, object data = null, [CallerMemberName] string callerName = "", [CallerFilePath] string fileName = "") { if (exception != null) DebugEmit(MessageForException(prefix, exception, data, callerName, fileName)); }
        public static void Exception(Exception exception, object data = null, [CallerMemberName] string callerName = "", [CallerFilePath] string fileName = "")                                            { Exception(null, exception, data, callerName, fileName); }
        public static void Exception(string prefix, Exception exception, object data = null, [CallerMemberName] string callerName = "", [CallerFilePath] string fileName = "")                             { if (exception != null) Emit(MessageForException(prefix, exception, data, callerName, fileName)); }
        public static void CriticalException(Exception exception, object data = null, [CallerMemberName] string callerName = "", [CallerFilePath] string fileName = "")                                    { CriticalException(null, exception, data, callerName, fileName); }
        public static void CriticalException(string prefix, Exception exception, object data = null, [CallerMemberName] string callerName = "", [CallerFilePath] string fileName = "")                     { if (exception != null) Critical(MessageForException(prefix, exception, data, callerName, fileName)); }

        static string MessageForException(string prefix, Exception exception, object data, string callerName, string fileName)
        {
            var type     = exception.GetType();
            var typeName = type?.FullName ?? "<unknown>";
            var message  = exception.Message;
            var header   = prefix ?? $"unhandled exception {typeName} in {callerName} ({fileName})";

            var dataString  = data == null ? string.Empty : $"\n{ObjectDumper.GetText(data, "\t")}";
            var stackString = string.Join("\n", exception.StackTrace.Split('\n').Select(x => $"\t{x}"));
            return $"{header}\n\ttype: {typeName}{dataString}\n\tmessage: {message}\n\tstack: {stackString}";
        }
    }
}
