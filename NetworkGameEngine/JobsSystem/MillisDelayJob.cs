using NetworkGameEngine.JobsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.JobsSystem
{
    public class MillisDelayJob : Job
    {
        private Func<bool> predicate;

        public override bool IsCompleted => predicate.Invoke();

        public MillisDelayJob(int millis)
        {
            long startTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            this.predicate = () =>
            {
                float timer = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTimeStamp;
                return timer >= millis;
            };
        }
    }
}
