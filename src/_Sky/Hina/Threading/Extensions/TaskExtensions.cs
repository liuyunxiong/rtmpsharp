using System.Threading.Tasks;

// csharp: hina/threading/taskextensions.cs [snipped]
namespace Hina.Threading
{
    static class TaskExtensions
    {
        public static void Forget(this Task task) { }
    }
}
