using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using Hina.Linq;

// csharp: hina/threading/taskcallbackmanager.cs [snipped]
namespace Hina.Threading
{
    class TaskCallbackManager<TKey, TValue>
    {
        readonly ConcurrentDictionary<TKey, TaskCompletionSource<TValue>> callbacks = new ConcurrentDictionary<TKey, TaskCompletionSource<TValue>>();

        public Task<TValue> Create(TKey key)
        {
            Check.NotNull(key);

            return callbacks
                .GetOrAdd(key, k => new TaskCompletionSource<TValue>(TaskCreationOptions.RunContinuationsAsynchronously))
                .Task;
        }

        public bool Remove(TKey key)
        {
            Check.NotNull(key);

            return callbacks.TryRemove(key, out var callback);
        }

        public void SetResult(TKey key, TValue result)
        {
            Check.NotNull(key);

            if (callbacks.TryRemove(key, out var callback))
                callback.TrySetResult(result);
        }

        public void SetException(TKey key, Exception exception)
        {
            Check.NotNull(key);

            if (callbacks.TryRemove(key, out var callback))
                callback.TrySetException(exception);
        }

        public void SetResultForAll(TValue result)
        {
            var sources = callbacks.MapArray(x => x.Value);
            callbacks.Clear();

            foreach (var source in sources)
                source.TrySetResult(result);
        }

        public void SetExceptionForAll(Exception exception)
        {
            var sources = callbacks.MapArray(x => x.Value);
            callbacks.Clear();

            foreach (var source in sources)
                source.TrySetException(exception);
        }

        public void Clear()
        {
            callbacks.Clear();
        }
    }
}
