using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine
{
    public class Time
    {
        private int m_tick = 0;
        private long m_startTickTime;
        public readonly long FixedDeltaTimeMillis = 100;
        public readonly float FixedDeltaTimeSeconds = 0.1f;

        public int Tick => m_tick;

        public Time(int tickInterval)
        {
            FixedDeltaTimeMillis = tickInterval;
            FixedDeltaTimeSeconds = tickInterval / 1000f;
            m_startTickTime = Milliseconds;
        }

        public void NextTick()
        {
            m_tick++;
            m_startTickTime = Milliseconds;
        }

        internal int CalculateSleepTime()
        {
            return (int)(FixedDeltaTimeMillis - (Milliseconds - m_startTickTime));
        }

        public static long Milliseconds => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
}
