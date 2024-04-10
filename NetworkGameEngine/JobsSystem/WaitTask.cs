using NetworkGameEngine.JobsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.JobsSystem
{
    internal class WaitTask : Job
    {
        private Task m_task;

        public override bool IsCompleted => m_task.IsCompleted;

        public WaitTask(Task task)
        {
            m_task = task;
        }
    }
}
