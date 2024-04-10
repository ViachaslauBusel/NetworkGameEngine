using NetworkGameEngine.JobsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.JobsSystem
{
    public class JobContainer : Job
    {
        private List<Task> _tasks = new List<Task>();

        public override bool IsCompleted => _tasks.All(task => task.IsCompleted);

        public void AddTask(Task task) => _tasks.Add(task);
    }

    public struct JobContainer<T> 
    {
        private List<Task<T>> _tasks = new List<Task<T>>();

        public JobContainer()
        {
        }

        public void AddTask(Task<T> task) => _tasks.Add(task);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public async Job<T[]> WaitAll()
        {
            return await Task.WhenAll(_tasks);
        }
    }
}
