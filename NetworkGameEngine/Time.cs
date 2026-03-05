namespace NetworkGameEngine
{
    public class Time
    {
        private int m_tick = 0;
        private long m_startTickTime;
        private long m_lastTickTime;
        private int m_deltaTime;
        public readonly long FixedDeltaTimeMillis = 100;
        public readonly float FixedDeltaTimeSeconds = 0.1f;

        public int Tick => m_tick;
        public int DeltaTime => m_deltaTime;
        public float DeltaTimeSeconds => m_deltaTime / 1000f;

        public Time(int tickInterval)
        {
            FixedDeltaTimeMillis = tickInterval;
            FixedDeltaTimeSeconds = tickInterval / 1000f;
            m_startTickTime = Milliseconds;
            m_lastTickTime = Milliseconds;
        }

        internal void NextTick()
        {
            m_tick++;
            m_startTickTime = Milliseconds;
            m_deltaTime = (int)(m_startTickTime - m_lastTickTime);
            m_lastTickTime = m_startTickTime;
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
