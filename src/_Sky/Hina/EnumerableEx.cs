using System;

// csharp: hina/enumerableex.cs [snipped]
namespace Hina
{
    static class EnumerableEx
    {
        public static T[] Range<T>(int count, Func<T> create)
        {
            Check.NotNull(create);

            var array = new T[count];

            for (var i = 0; i < count; i++)
                array[i] = create();

            return array;
        }
    }
}
