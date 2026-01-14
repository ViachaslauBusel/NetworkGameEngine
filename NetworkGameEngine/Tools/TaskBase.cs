using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
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

        // Replace blocking Task.Run waiter with a TCS to remove scheduling latency
        private TaskCompletionSource<bool> _tcs; // lazily created

        public Task<bool> Wait()
        {
            // Fast-path if already completed
            if (IsCompleted)
            {
                return Task.FromResult(IsCompletedSuccessfully);
            }

            // Create the TCS lazily and thread-safely
            lock (m_locker)
            {
                if (IsCompleted)
                {
                    return Task.FromResult(IsCompletedSuccessfully);
                }
                _tcs ??= new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                return _tcs.Task;
            }
        }

        public void Abort()
        {
            lock (m_locker)
            {
                if (IsCompleted) return;
                IsCompleted = true;
                IsCompletedSuccessfully = false;
                IsFaulted = true;
                IsCanceled = true;
                // Wake any legacy waiters
                Monitor.Pulse(m_locker);
                // Complete async waiters without extra scheduling
                _tcs?.TrySetResult(false);
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
                // Wake any legacy waiters
                Monitor.Pulse(m_locker);
                // Complete async waiters without extra scheduling
                _tcs?.TrySetResult(result);
            }
        }
    }
}
