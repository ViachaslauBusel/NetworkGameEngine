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

        internal int CalculateTimeFromTick()
        {
            return (int)(Milliseconds - m_startTickTime);
        }

        public static long Milliseconds => Environment.TickCount64;

        public static DateTime DateTime => DateTime.Now;

        public static long CalculateMsUntil(DateTime futureTime)
        {
            return (long)(futureTime - DateTime).TotalMilliseconds;
        }

        public static long ConvertDateTimeToMs(DateTime time)
        {
            return Milliseconds + CalculateMsUntil(time);
        }

        public static long AddSecondsToMs(int totalSeconds)
        {
            return Milliseconds + (totalSeconds * 1000);
        }

        public static DateTime AddSecondsToDateTime(int totalSeconds)
        {
            return DateTime.AddSeconds(totalSeconds);
        }

        public static DateTime AddMsToDateTime(long totalMs)
        {
            return DateTime.AddMilliseconds(totalMs);
        }
    }
}
