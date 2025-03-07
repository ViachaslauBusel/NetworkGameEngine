using NetworkGameEngine.JobsSystem;
using System;

namespace NetworkGameEngine.JobsSystem
{
    public class WaitUntilJob : Job
    {
        private Func<bool> predicate;

        public override bool IsCompleted => predicate.Invoke();

        public WaitUntilJob(Func<bool> predicate)
        {
            this.predicate = predicate;
        }
    }
}
