using System;
using System.Collections.Generic;

// csharp: hina/linq/buffer.cs [snipped]
namespace Hina.Linq
{
    class Buffer<T>
    {
        const int DefaultCapacity = 4;

        T[] items;
        int count;

        public Buffer(int count)
        {
            if (count > 0)
                items = new T[count];
        }

        public Buffer(IEnumerable<T> source)
        {
            if (source is ICollection<T> collection)
            {
                count = collection.Count;

                if (count > 0)
                {
                    items = new T[count];
                    collection.CopyTo(items, 0);
                }
            }
            else
            {
                foreach (var item in source)
                    Append(item);
            }
        }

        public void Append(T item)
        {
            if (items == null)
            {
                items = new T[DefaultCapacity];
            }
            else if (items.Length == count)
            {
                var newItems = new T[checked(count * 2)];
                Array.Copy(items, 0, newItems, 0, count);
                items = newItems;
            }

            items[count] = item;
            count++;
        }

        public T[] ToArray()
        {
            if (count == 0)
                return new T[0];

            if (items.Length == count)
                return items;

            var result = new T[count];
            Array.Copy(items, 0, result, 0, count);
            return result;
        }
    }
}
