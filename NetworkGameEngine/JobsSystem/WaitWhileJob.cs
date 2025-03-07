using System;

namespace NetworkGameEngine.JobsSystem
{
    public class WaitWhileJob : Job
    {
        private Func<bool> predicate;

        public override bool IsCompleted => !predicate.Invoke();

        public WaitWhileJob(Func<bool> predicate)
        {
            this.predicate = predicate;
        }
    }
}
