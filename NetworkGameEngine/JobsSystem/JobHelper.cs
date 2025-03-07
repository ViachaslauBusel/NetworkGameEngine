using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.JobsSystem
{
    public partial class Job
    {
        public static MillisDelayJob Delay(int millis)
        {
            return new MillisDelayJob(millis);
        }

        public static SecondsDelayJob Delay(float seconds)
        {
            return new SecondsDelayJob(seconds);
        }
    }
}
