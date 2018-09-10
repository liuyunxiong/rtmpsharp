using System.Collections.Generic;

// csharp: hina/checkdebug.cs [snipped]
namespace Hina
{
    static class CheckDebug
    {
        public static T NotNull<T>(T obj)
            => Check.NotNull(obj);

        public static void NotNull<T1, T2>(T1 t1, T2 t2)
            => Check.NotNull(t1, t2);

        public static void NotNull<T1, T2, T3>(T1 t1, T2 t2, T3 t3)
            => Check.NotNull(t1, t2, t3);

        public static void NotNull<T1, T2, T3, T4>(T1 t1, T2 t2, T3 t3, T4 t4)
            => Check.NotNull(t1, t2, t3, t4);

        public static void NotNull<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5)
            => Check.NotNull(t1, t2, t3, t4, t5);

        public static void NotNull<T1, T2, T3, T4, T5, T6>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6)
            => Check.NotNull(t1, t2, t3, t4, t5, t6);

        public static void NotNull<T1, T2, T3, T4, T5, T6, T7>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7)
            => Check.NotNull(t1, t2, t3, t4, t5, t6, t7);

        public static void NotNull<T1, T2, T3, T4, T5, T6, T7, T8>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8)
            => Check.NotNull(t1, t2, t3, t4, t5, t6, t7, t8);

        public static void NotNull<T1, T2, T3, T4, T5, T6, T7, T8, T9>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9)
            => Check.NotNull(t1, t2, t3, t4, t5, t6, t7, t8, t9);

        public static void NotNull<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10)
            => Check.NotNull(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

        public static IReadOnlyList<T> NotEmpty<T>(IReadOnlyList<T> obj)
            => Check.NotEmpty(obj);

        public static void NotEmpty<T1, T2>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2)
            => Check.NotEmpty(t1, t2);

        public static void NotEmpty<T1, T2, T3>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3)
            => Check.NotEmpty(t1, t2, t3);

        public static void NotEmpty<T1, T2, T3, T4>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4)
            => Check.NotEmpty(t1, t2, t3, t4);

        public static void NotEmpty<T1, T2, T3, T4, T5>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5)
            => Check.NotEmpty(t1, t2, t3, t4, t5);

        public static void NotEmpty<T1, T2, T3, T4, T5, T6>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6)
            => Check.NotEmpty(t1, t2, t3, t4, t5, t6);

        public static void NotEmpty<T1, T2, T3, T4, T5, T6, T7>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7)
            => Check.NotEmpty(t1, t2, t3, t4, t5, t6, t7);

        public static void NotEmpty<T1, T2, T3, T4, T5, T6, T7, T8>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8)
            => Check.NotEmpty(t1, t2, t3, t4, t5, t6, t7, t8);

        public static void NotEmpty<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8, IReadOnlyList<T9> t9)
            => Check.NotEmpty(t1, t2, t3, t4, t5, t6, t7, t8, t9);

        public static void NotEmpty<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8, IReadOnlyList<T9> t9, IReadOnlyList<T1> t10)
            => Check.NotEmpty(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);

        public static IReadOnlyList<T> NoneNull<T>(IReadOnlyList<T> obj)
            => Check.NoneNull(obj);

        public static void NoneNull<T1, T2>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2)
            => Check.NoneNull(t1, t2);

        public static void NoneNull<T1, T2, T3>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3)
            => Check.NoneNull(t1, t2, t3);

        public static void NoneNull<T1, T2, T3, T4>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4)
            => Check.NoneNull(t1, t2, t3, t4);

        public static void NoneNull<T1, T2, T3, T4, T5>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5)
            => Check.NoneNull(t1, t2, t3, t4, t5);

        public static void NoneNull<T1, T2, T3, T4, T5, T6>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6)
            => Check.NoneNull(t1, t2, t3, t4, t5, t6);

        public static void NoneNull<T1, T2, T3, T4, T5, T6, T7>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7)
            => Check.NoneNull(t1, t2, t3, t4, t5, t6, t7);

        public static void NoneNull<T1, T2, T3, T4, T5, T6, T7, T8>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8)
            => Check.NoneNull(t1, t2, t3, t4, t5, t6, t7, t8);

        public static void NoneNull<T1, T2, T3, T4, T5, T6, T7, T8, T9>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8, IReadOnlyList<T9> t9)
            => Check.NoneNull(t1, t2, t3, t4, t5, t6, t7, t8, t9);

        public static void NoneNull<T1, T2, T3, T4, T5, T6, T7, T8, T9, T10>(IReadOnlyList<T1> t1, IReadOnlyList<T2> t2, IReadOnlyList<T3> t3, IReadOnlyList<T4> t4, IReadOnlyList<T5> t5, IReadOnlyList<T6> t6, IReadOnlyList<T7> t7, IReadOnlyList<T8> t8, IReadOnlyList<T9> t9, IReadOnlyList<T1> t10)
            => Check.NoneNull(t1, t2, t3, t4, t5, t6, t7, t8, t9, t10);
    }
}
