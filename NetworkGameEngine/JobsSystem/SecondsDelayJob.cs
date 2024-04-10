using NetworkGameEngine.JobsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.JobsSystem
{
    public sealed class SecondsDelayJob : Job
    {
        private Func<bool> predicate;

        public override bool IsCompleted => predicate.Invoke();

        public SecondsDelayJob(float second)
        {
            long startTimeStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            this.predicate = () =>
            {
                float timer = (DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startTimeStamp) / 1_000.0f;
                return timer >= second;
            };
        }
    }
}
