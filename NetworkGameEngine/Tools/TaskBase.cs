using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.Tools
{
    public abstract class TaskBase
    {
        private const long MAX_ATTEMPT_TIME = 10_000;

        private object m_locker = new object();

        public bool IsCompleted { get; private set; } = false;
        public bool IsCompletedSuccessfully { get; private set; } = false;
        public bool IsCanceled { get; private set; } = false;
        public bool IsFaulted { get; private set; } = false;




        public Task<bool> Wait() => Task.Run(() =>
        {
            lock (m_locker)
            {
                long startStamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                while (!IsCompleted)
                {
                    Monitor.Wait(m_locker, 100);
                    if ((DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - startStamp) > MAX_ATTEMPT_TIME)
                    {
                        Abort();
                    }
                }
                return IsCompletedSuccessfully;
            }
        });


        public void Abort()
        {
            lock (m_locker)
            {
                if (IsCompleted) return;
                IsCompleted = true;
                IsCompletedSuccessfully = false;
                IsFaulted = true;
                IsCanceled = true;
                Monitor.Pulse(m_locker);
            }
        }
        public void Completed(bool result)
        {
            lock (m_locker)
            {
                if (IsCompleted) return;
                IsCompleted = true;
                IsCompletedSuccessfully = result;
                IsFaulted = !result;
                IsCanceled = false;
                Monitor.Pulse(m_locker);
            }
        }
    }
}
