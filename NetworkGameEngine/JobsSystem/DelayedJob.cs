using NetworkGameEngine.JobsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.JobsSystem
{
    public class DelayedJob : Job
    {
        private Func<bool> _predicate;

        public override bool IsCompleted => _predicate.Invoke();

        public DelayedJob(int waitTime, Action job)
        {
            bool isCompleted = false;
            long endWaitTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + waitTime;
            _predicate = () =>
            {
                if(isCompleted) return true;
                if (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() >= endWaitTime)
                {
                    job.Invoke();
                    isCompleted = true;
                    return true;
                }
                return false;
            };
        }

        public void Cancel()
        {
            _predicate = () => true;
        }
    }
}
