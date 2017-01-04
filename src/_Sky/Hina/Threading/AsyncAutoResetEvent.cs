using System.Collections.Generic;
using System.Threading.Tasks;

// csharp: hina/threading/asyncautoresetevent.cs [snipped]
namespace Hina.Threading
{
    class AsyncAutoResetEvent
    {
        bool signaled;
        readonly Queue<TaskCompletionSource<object>> tasks = new Queue<TaskCompletionSource<object>>();

        public Task WaitAsync()
        {
            lock (tasks)
            {
                if (signaled)
                {
                    signaled = false;
                    return Task.CompletedTask;
                }

                var source = new TaskCompletionSource<object>();
                tasks.Enqueue(source);
                return source.Task;
            }
        }

        public void Set()
        {
            TaskCompletionSource<object> task = null;

            lock (tasks)
            {
                if (tasks.Count > 0)
                    task = tasks.Dequeue();
                else if (signaled == false)
                    signaled = true;
            }

            task?.SetResult(true);
        }
    }
}
