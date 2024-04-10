using NetworkGameEngine.JobsSystem;
using System;

namespace NetworkGameEngine.JobsSystem
{
    public class WaitUntil : Job
    {
        private Func<bool> predicate;

        public override bool IsCompleted => predicate.Invoke();

        public WaitUntil(Func<bool> predicate)
        {
            this.predicate = predicate;
        }
    }
}
