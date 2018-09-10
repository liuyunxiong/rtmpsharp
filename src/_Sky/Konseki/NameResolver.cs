using System;
using System.Text.RegularExpressions;

// csharp: konseki/kon/nameresolver.static-facade.cs [snipped]
namespace Konseki
{
    static class NameResolver
    {
        public static string GetNameFullNetFramework()
        {
#if DEBUG && COREFX_HAS_STACKFRAME
            // 3: SomeClass.SomeMethod() <----- we want this!
            // 2: StaticLogger.Debug("hello")
            // 1: StaticLogger.Log()
            // 0: NameResolver.GetName()

            // new StackFrame(
            //     skip `n` frames,
            //     no file name needed)
            var frame = new StackFrame(3, false);
            var method = frame.GetMethod();
            var type = method.DeclaringType;

            return type != null
                ? $"{type.FullName}.{method.Name}"
                : method.Name;
#else
            return "_";
#endif
        }


        static readonly Regex CoreClrStackTraceFunctionNameRegex = new Regex(@" +at (?<functionName>.+?)\(", RegexOptions.Compiled);

        // todo: temporary workaround until corefx lets us construct a stackframe / stacktrace
        public static string GetName()
        {
            // 0:   at System.Environment.GetStackTrace(Exception e, Boolean needFileInfo)
            // 1:   at System.Environment.get_StackTrace()
            // 2:   at Konseki.NameResolver.GetName() in D:\code\sweet\vs\source\konseki\NameResolver.cs:line 16
            // 3:   at Konseki.Kon.Log(LogLevel level, String message) in D:\code\sweet\vs\source\konseki\Kon.cs:line 26
            // 4:   at Konseki.Kon.Debug(String message) in D:\code\sweet\vs\source\konseki\Kon.cs:line 15
            // 5:   at netcorespike.Program.Main(String[] args) in D:\code\sweet\vs\source\netcorespike\Program.cs:line 14
            // 6:   at System.RuntimeMethodHandle.InvokeMethod(Object target, Object[] arguments, Signature sig, Boolean constructor)
            var frames = Environment.StackTrace.Split('\n');

            if (frames.Length <= 5)
                return "_";

            var frame = frames[5];
            var match = CoreClrStackTraceFunctionNameRegex.Match(frame);
            var name  = match.Groups["functionName"].Value;

            return name.Trim();
        }
    }
}

