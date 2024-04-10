using System;

namespace NetworkGameEngine.JobsSystem
{
    public class WaitWhile : Job
    {
        private Func<bool> predicate;

        public override bool IsCompleted => !predicate.Invoke();

        public WaitWhile(Func<bool> predicate)
        {
            this.predicate = predicate;
        }
    }
}
