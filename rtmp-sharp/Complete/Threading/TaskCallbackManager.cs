using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

// astralfoxy:complete/threading/taskcallbackmanager.cs
namespace Complete.Threading
{
    class TaskCallbackManager<K, V>
    {
        readonly ConcurrentDictionary<K, TaskCompletionSource<V>> callbacks;

        public TaskCallbackManager()
        {
            callbacks = new ConcurrentDictionary<K, TaskCompletionSource<V>>();
        }

        public Task<V> Create(K key)
        {
            var taskCompletionSource = callbacks.GetOrAdd(key, k => new TaskCompletionSource<V>());
            return taskCompletionSource.Task;
        }

        public bool Remove(K key)
        {
            TaskCompletionSource<V> callback;
            return callbacks.TryRemove(key, out callback);
        }

        public void SetResult(K key, V result)
        {
            TaskCompletionSource<V> callback;
            if (callbacks.TryRemove(key, out callback))
                callback.TrySetResult(result);
        }

        public void SetException(K key, Exception exception)
        {
            TaskCompletionSource<V> callback;
            if (callbacks.TryRemove(key, out callback))
                callback.TrySetException(exception);
        }

        public void SetResultForAll(V result)
        {
            var callbacks = this.callbacks.Select(x => x.Value).ToArray();
            this.callbacks.Clear();

            foreach (var callback in callbacks)
                callback.TrySetResult(result);
        }

        public void SetExceptionForAll(Exception exception)
        {
            var callbacks = this.callbacks.Select(x => x.Value).ToArray();
            this.callbacks.Clear();

            foreach (var callback in callbacks)
                callback.TrySetException(exception);
        }

        public void Clear()
        {
            callbacks.Clear();
        }
    }
}
