using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Konseki;

// csharp: hina/check.cs [snipped]
namespace Hina
{
    static class Check
    {
        #region Utilities

        static void DebugBreak()
        {
#if DEBUG
            var traceLines = Environment.StackTrace.Split('\n').Select(x => $"    {x}");
            var trace = string.Join("\n", traceLines);

            Kon.Critical($"invalid null passed at\n{trace}");
            Debugger.Break();
#endif
        }

        #endregion

        #region NotNull<TArgs...>

        public static T NotNull<T>(T obj)
        {
            if (obj == null) throw CreateNullException(0);

            return obj;
        }

        public static void NotNull<T1, T2>(T1 t1, T2 t2)
        {
            if (t1 == null) throw CreateNullException(0);
            if (t2 == null) throw CreateNullException(1);
        }

        public static void NotNull<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
        {
            if (t1 == null) throw CreateNullException(0);
            if (t2 == null) throw CreateNullException(1);
            if (t3 == null) throw CreateNullException(2);
        }

        public static void NotNull<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4)
        {
            if (t1 == null) throw CreateNullException(0);
            if (t2 == null) throw CreateNullException(1);
            if (t3 == null) throw CreateNullException(2);
            if (t4 == null) throw CreateNullException(3);
        }

        public static void NotNull<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
        {
            if (t1 == null) throw CreateNullException(0);
            if (t2 == null) throw CreateNullException(1);
            if (t3 == null) throw CreateNullException(2);
            if (t4 == null) throw CreateNullException(3);
            if (t5 == null) throw CreateNullException(4);
        }

        public static void NotNull<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
        {
            if (t1 == null) throw CreateNullException(0);
            if (t2 == null) throw CreateNullException(1);
            if (t3 == null) throw CreateNullException(2);
            if (t4 == null) throw CreateNullException(3);
            if (t5 == null) throw CreateNullException(4);
            if (t6 == null) throw CreateNullException(5);
        }

        public static void NotNull<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
        {
            if (t1 == null) throw CreateNullException(0);
            if (t2 == null) throw CreateNullException(1);
            if (t3 == null) throw CreateNullException(2);
            if (t4 == null) throw CreateNullException(3);
            if (t5 == null) throw CreateNullException(4);
            if (t6 == null) throw CreateNullException(5);
            if (t7 == null) throw CreateNullException(6);
        }

        public static void NotNull<T1, T2, T3, T4, T5, T6, T7, T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
        {
            if (t1 == null) throw CreateNullException(0);
            if (t2 == null) throw CreateNullException(1);
            if (t3 == null) throw CreateNullException(2);
            if (t4 == null) throw CreateNullException(3);
            if (t5 == null) throw CreateNullException(4);
            if (t6 == null) throw CreateNullException(5);
            if (t7 == null) throw CreateNullException(6);
            if (t8 == null) throw CreateNullException(7);
        }

        public static void NotNull<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9)
        {
            if (t1 == null) throw CreateNullException(0);
            if (t2 == null) throw CreateNullException(1);
            if (t3 == null) throw CreateNullException(2);
            if (t4 == null) throw CreateNullException(3);
            if (t5 == null) throw CreateNullException(4);
            if (t6 == null) throw CreateNullException(5);
            if (t7 == null) throw CreateNullException(6);
            if (t8 == null) throw CreateNullException(7);
            if (t9 == null) throw CreateNullException(8);
        }

        public static void NotNull<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10)
        {
            if (t1 == null) throw CreateNullException(0);
            if (t2 == null) throw CreateNullException(1);
            if (t3 == null) throw CreateNullException(2);
            if (t4 == null) throw CreateNullException(3);
            if (t5 == null) throw CreateNullException(4);
            if (t6 == null) throw CreateNullException(5);
            if (t7 == null) throw CreateNullException(6);
            if (t8 == null) throw CreateNullException(7);
            if (t9 == null) throw CreateNullException(8);
            if (t10 == null) throw CreateNullException(9);
        }

        static Exception CreateNullException(int index)
        {
            DebugBreak();
            return new ArgumentNullException($"parameter-{index}", "one of the passed-in parameters is null");
        }

        #endregion

        #region NotEmpty<TArgs...>

        public static IReadOnlyList<T> NotEmpty<T>(IReadOnlyList<T> obj)
        {
            NotNull(obj);

            if (obj.Count == 0) throw CreateEmptyException(0);

            return obj;
        }

        public static void NotEmpty<T1, T2>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2)
        {
            NotNull(t1, t2);

            if (t1.Count == 0) throw CreateEmptyException(0);
            if (t2.Count == 0) throw CreateEmptyException(1);
        }

        public static void NotEmpty<T1, T2, T3>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3)
        {
            NotNull(t1, t2, t3);

            if (t1.Count == 0) throw CreateEmptyException(0);
            if (t2.Count == 0) throw CreateEmptyException(1);
            if (t3.Count == 0) throw CreateEmptyException(2);
        }

        public static void NotEmpty<T1, T2, T3, T4>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4)
        {
            NotNull(t1, t2, t3, t4);

            if (t1.Count == 0) throw CreateEmptyException(0);
            if (t2.Count == 0) throw CreateEmptyException(1);
            if (t3.Count == 0) throw CreateEmptyException(2);
            if (t4.Count == 0) throw CreateEmptyException(3);
        }

        public static void NotEmpty<T1, T2, T3, T4, T5>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5)
        {
            NotNull(t1, t2, t3, t4, t5);

            if (t1.Count == 0) throw CreateEmptyException(0);
            if (t2.Count == 0) throw CreateEmptyException(1);
            if (t3.Count == 0) throw CreateEmptyException(2);
            if (t4.Count == 0) throw CreateEmptyException(3);
            if (t5.Count == 0) throw CreateEmptyException(4);
        }

        public static void NotEmpty<T1, T2, T3, T4, T5, T6>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6)
        {
            NotNull(t1, t2, t3, t4, t5, t6);

            if (t1.Count == 0) throw CreateEmptyException(0);
            if (t2.Count == 0) throw CreateEmptyException(1);
            if (t3.Count == 0) throw CreateEmptyException(2);
            if (t4.Count == 0) throw CreateEmptyException(3);
            if (t5.Count == 0) throw CreateEmptyException(4);
            if (t6.Count == 0) throw CreateEmptyException(5);
        }

        public static void NotEmpty<T1, T2, T3, T4, T5, T6, T7>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7);

            if (t1.Count == 0) throw CreateEmptyException(0);
            if (t2.Count == 0) throw CreateEmptyException(1);
            if (t3.Count == 0) throw CreateEmptyException(2);
            if (t4.Count == 0) throw CreateEmptyException(3);
            if (t5.Count == 0) throw CreateEmptyException(4);
            if (t6.Count == 0) throw CreateEmptyException(5);
            if (t7.Count == 0) throw CreateEmptyException(6);
        }

        public static void NotEmpty<T1, T2, T3, T4, T5, T6, T7, T8>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7, t8);

            if (t1.Count == 0) throw CreateEmptyException(0);
            if (t2.Count == 0) throw CreateEmptyException(1);
            if (t3.Count == 0) throw CreateEmptyException(2);
            if (t4.Count == 0) throw CreateEmptyException(3);
            if (t5.Count == 0) throw CreateEmptyException(4);
            if (t6.Count == 0) throw CreateEmptyException(5);
            if (t7.Count == 0) throw CreateEmptyException(6);
            if (t8.Count == 0) throw CreateEmptyException(7);
        }

        public static void NotEmpty<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8, IReadOnlyList<T9> t9)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7, t8, t9);

            if (t1.Count == 0) throw CreateEmptyException(0);
            if (t2.Count == 0) throw CreateEmptyException(1);
            if (t3.Count == 0) throw CreateEmptyException(2);
            if (t4.Count == 0) throw CreateEmptyException(3);
            if (t5.Count == 0) throw CreateEmptyException(4);
            if (t6.Count == 0) throw CreateEmptyException(5);
            if (t7.Count == 0) throw CreateEmptyException(6);
            if (t8.Count == 0) throw CreateEmptyException(7);
            if (t9.Count == 0) throw CreateEmptyException(8);
        }

        public static void NotEmpty<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8, IReadOnlyList<T9> t9, IReadOnlyList<T10> t10)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

            if (t1.Count == 0) throw CreateEmptyException(0);
            if (t2.Count == 0) throw CreateEmptyException(1);
            if (t3.Count == 0) throw CreateEmptyException(2);
            if (t4.Count == 0) throw CreateEmptyException(3);
            if (t5.Count == 0) throw CreateEmptyException(4);
            if (t6.Count == 0) throw CreateEmptyException(5);
            if (t7.Count == 0) throw CreateEmptyException(6);
            if (t8.Count == 0) throw CreateEmptyException(7);
            if (t9.Count == 0) throw CreateEmptyException(8);
            if (t10.Count == 0) throw CreateEmptyException(9);
        }

        static Exception CreateEmptyException(int index)
        {
            DebugBreak();
            return new ArgumentException("one of the passed-in parameters is an empty list", $"parameter-{index}");
        }

        #endregion

        #region NotEmpty<string>

        public static string NotEmpty(string obj)
        {
            NotNull(obj);

            if (obj.Length == 0) throw CreateEmptyStringException(0);

            return obj;
        }

        public static void NotEmpty(string t1, string t2)
        {
            NotNull(t1, t2);

            if (t1.Length == 0) throw CreateEmptyStringException(0);
            if (t2.Length == 0) throw CreateEmptyStringException(1);
        }

        public static void NotEmpty(string t1, string t2, string t3)
        {
            NotNull(t1, t2, t3);

            if (t1.Length == 0) throw CreateEmptyStringException(0);
            if (t2.Length == 0) throw CreateEmptyStringException(1);
            if (t3.Length == 0) throw CreateEmptyStringException(2);
        }

        public static void NotEmpty(string t1, string t2, string t3, string t4)
        {
            NotNull(t1, t2, t3, t4);

            if (t1.Length == 0) throw CreateEmptyStringException(0);
            if (t2.Length == 0) throw CreateEmptyStringException(1);
            if (t3.Length == 0) throw CreateEmptyStringException(2);
            if (t4.Length == 0) throw CreateEmptyStringException(3);
        }

        public static void NotEmpty(string t1, string t2, string t3, string t4, string t5)
        {
            NotNull(t1, t2, t3, t4, t5);

            if (t1.Length == 0) throw CreateEmptyStringException(0);
            if (t2.Length == 0) throw CreateEmptyStringException(1);
            if (t3.Length == 0) throw CreateEmptyStringException(2);
            if (t4.Length == 0) throw CreateEmptyStringException(3);
            if (t5.Length == 0) throw CreateEmptyStringException(4);
        }

        public static void NotEmpty(string t1, string t2, string t3, string t4, string t5, string t6)
        {
            NotNull(t1, t2, t3, t4, t5, t6);

            if (t1.Length == 0) throw CreateEmptyStringException(0);
            if (t2.Length == 0) throw CreateEmptyStringException(1);
            if (t3.Length == 0) throw CreateEmptyStringException(2);
            if (t4.Length == 0) throw CreateEmptyStringException(3);
            if (t5.Length == 0) throw CreateEmptyStringException(4);
            if (t6.Length == 0) throw CreateEmptyStringException(5);
        }

        public static void NotEmpty(string t1, string t2, string t3, string t4, string t5, string t6, string t7)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7);

            if (t1.Length == 0) throw CreateEmptyStringException(0);
            if (t2.Length == 0) throw CreateEmptyStringException(1);
            if (t3.Length == 0) throw CreateEmptyStringException(2);
            if (t4.Length == 0) throw CreateEmptyStringException(3);
            if (t5.Length == 0) throw CreateEmptyStringException(4);
            if (t6.Length == 0) throw CreateEmptyStringException(5);
            if (t7.Length == 0) throw CreateEmptyStringException(6);
        }

        public static void NotEmpty(string t1, string t2, string t3, string t4, string t5, string t6, string t7, string t8)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7, t8);

            if (t1.Length == 0) throw CreateEmptyStringException(0);
            if (t2.Length == 0) throw CreateEmptyStringException(1);
            if (t3.Length == 0) throw CreateEmptyStringException(2);
            if (t4.Length == 0) throw CreateEmptyStringException(3);
            if (t5.Length == 0) throw CreateEmptyStringException(4);
            if (t6.Length == 0) throw CreateEmptyStringException(5);
            if (t7.Length == 0) throw CreateEmptyStringException(6);
            if (t8.Length == 0) throw CreateEmptyStringException(7);
        }

        public static void NotEmpty(string t1, string t2, string t3, string t4, string t5, string t6, string t7, string t8, string t9)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7, t8, t9);

            if (t1.Length == 0) throw CreateEmptyStringException(0);
            if (t2.Length == 0) throw CreateEmptyStringException(1);
            if (t3.Length == 0) throw CreateEmptyStringException(2);
            if (t4.Length == 0) throw CreateEmptyStringException(3);
            if (t5.Length == 0) throw CreateEmptyStringException(4);
            if (t6.Length == 0) throw CreateEmptyStringException(5);
            if (t7.Length == 0) throw CreateEmptyStringException(6);
            if (t8.Length == 0) throw CreateEmptyStringException(7);
            if (t9.Length == 0) throw CreateEmptyStringException(8);
        }

        public static void NotEmpty(string t1, string t2, string t3, string t4, string t5, string t6, string t7, string t8, string t9, string t10)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

            if (t1.Length == 0) throw CreateEmptyStringException(0);
            if (t2.Length == 0) throw CreateEmptyStringException(1);
            if (t3.Length == 0) throw CreateEmptyStringException(2);
            if (t4.Length == 0) throw CreateEmptyStringException(3);
            if (t5.Length == 0) throw CreateEmptyStringException(4);
            if (t6.Length == 0) throw CreateEmptyStringException(5);
            if (t7.Length == 0) throw CreateEmptyStringException(6);
            if (t8.Length == 0) throw CreateEmptyStringException(7);
            if (t9.Length == 0) throw CreateEmptyStringException(8);
            if (t10.Length == 0) throw CreateEmptyStringException(9);
        }

        static Exception CreateEmptyStringException(int index)
        {
            DebugBreak();
            return new ArgumentException("one of the passed-in parameters is an empty string", $"parameter-{index}");
        }

        #endregion

        #region NoneNull<TArgs...>

        public static IReadOnlyList<T> NoneNull<T>(IReadOnlyList<T> obj)
        {
            NotNull(obj);

            if (HasNullElement(obj)) throw CreateNullElementException(0);

            return obj;
        }

        public static void NoneNull<T1, T2>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2)
        {
            NotNull(t1, t2);

            if (HasNullElement(t1)) throw CreateNullElementException(0);
            if (HasNullElement(t2)) throw CreateNullElementException(1);
        }

        public static void NoneNull<T1, T2, T3>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3)
        {
            NotNull(t1, t2, t3);

            if (HasNullElement(t1)) throw CreateNullElementException(0);
            if (HasNullElement(t2)) throw CreateNullElementException(1);
            if (HasNullElement(t3)) throw CreateNullElementException(2);
        }

        public static void NoneNull<T1, T2, T3, T4>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4)
        {
            NotNull(t1, t2, t3, t4);

            if (HasNullElement(t1)) throw CreateNullElementException(0);
            if (HasNullElement(t2)) throw CreateNullElementException(1);
            if (HasNullElement(t3)) throw CreateNullElementException(2);
            if (HasNullElement(t4)) throw CreateNullElementException(3);
        }

        public static void NoneNull<T1, T2, T3, T4, T5>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5)
        {
            NotNull(t1, t2, t3, t4, t5);

            if (HasNullElement(t1)) throw CreateNullElementException(0);
            if (HasNullElement(t2)) throw CreateNullElementException(1);
            if (HasNullElement(t3)) throw CreateNullElementException(2);
            if (HasNullElement(t4)) throw CreateNullElementException(3);
            if (HasNullElement(t5)) throw CreateNullElementException(4);
        }

        public static void NoneNull<T1, T2, T3, T4, T5, T6>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6)
        {
            NotNull(t1, t2, t3, t4, t5, t6);

            if (HasNullElement(t1)) throw CreateNullElementException(0);
            if (HasNullElement(t2)) throw CreateNullElementException(1);
            if (HasNullElement(t3)) throw CreateNullElementException(2);
            if (HasNullElement(t4)) throw CreateNullElementException(3);
            if (HasNullElement(t5)) throw CreateNullElementException(4);
            if (HasNullElement(t6)) throw CreateNullElementException(5);
        }

        public static void NoneNull<T1, T2, T3, T4, T5, T6, T7>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7);

            if (HasNullElement(t1)) throw CreateNullElementException(0);
            if (HasNullElement(t2)) throw CreateNullElementException(1);
            if (HasNullElement(t3)) throw CreateNullElementException(2);
            if (HasNullElement(t4)) throw CreateNullElementException(3);
            if (HasNullElement(t5)) throw CreateNullElementException(4);
            if (HasNullElement(t6)) throw CreateNullElementException(5);
            if (HasNullElement(t7)) throw CreateNullElementException(6);
        }

        public static void NoneNull<T1, T2, T3, T4, T5, T6, T7, T8>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7, t8);

            if (HasNullElement(t1)) throw CreateNullElementException(0);
            if (HasNullElement(t2)) throw CreateNullElementException(1);
            if (HasNullElement(t3)) throw CreateNullElementException(2);
            if (HasNullElement(t4)) throw CreateNullElementException(3);
            if (HasNullElement(t5)) throw CreateNullElementException(4);
            if (HasNullElement(t6)) throw CreateNullElementException(5);
            if (HasNullElement(t7)) throw CreateNullElementException(6);
            if (HasNullElement(t8)) throw CreateNullElementException(7);
        }

        public static void NoneNull<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8, IReadOnlyList<T9> t9)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7, t8, t9);

            if (HasNullElement(t1)) throw CreateNullElementException(0);
            if (HasNullElement(t2)) throw CreateNullElementException(1);
            if (HasNullElement(t3)) throw CreateNullElementException(2);
            if (HasNullElement(t4)) throw CreateNullElementException(3);
            if (HasNullElement(t5)) throw CreateNullElementException(4);
            if (HasNullElement(t6)) throw CreateNullElementException(5);
            if (HasNullElement(t7)) throw CreateNullElementException(6);
            if (HasNullElement(t8)) throw CreateNullElementException(7);
            if (HasNullElement(t9)) throw CreateNullElementException(8);
        }

        public static void NoneNull<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8, IReadOnlyList<T9> t9, IReadOnlyList<T10> t10)
        {
            NotNull(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

            if (HasNullElement(t1)) throw CreateNullElementException(0);
            if (HasNullElement(t2)) throw CreateNullElementException(1);
            if (HasNullElement(t3)) throw CreateNullElementException(2);
            if (HasNullElement(t4)) throw CreateNullElementException(3);
            if (HasNullElement(t5)) throw CreateNullElementException(4);
            if (HasNullElement(t6)) throw CreateNullElementException(5);
            if (HasNullElement(t7)) throw CreateNullElementException(6);
            if (HasNullElement(t8)) throw CreateNullElementException(7);
            if (HasNullElement(t9)) throw CreateNullElementException(8);
            if (HasNullElement(t10)) throw CreateNullElementException(9);
        }

        static bool HasNullElement<T>(IEnumerable<T> list)
        {
            foreach (var element in list)
            {
                if (element == null)
                    return true;
            }

            return false;
        }

        static Exception CreateNullElementException(int index)
        {
            DebugBreak();
            return new ArgumentException("one of the passed-in parameters contains a null element", $"parameter-{index}");
        }

        #endregion
    }
}