using NetworkGameEngine.Interfaces;

namespace NetworkGameEngine.Tools
{
    internal sealed class MainThreadDelayDispatcher : IMainThreadUpdatableService, IMultiThreadUpdatableService
    {
        private readonly Dictionary<TimedTaskScheduler, long> _scheduledWakeUpTimeByScheduler = new Dictionary<TimedTaskScheduler, long>();
        private readonly List<TimedTaskScheduler> _readySchedulers = new List<TimedTaskScheduler>();
        private readonly List<TimedTaskScheduler> _schedulersToRemove = new List<TimedTaskScheduler>();
        private readonly object _timedSchedulersLock = new object();

        // Быстрый путь: если ближайшее время еще не наступило — даже не заходим в lock.
        private long _nextWakeUpTimeMs = long.MaxValue;

        /// <summary>
        /// Подготовка к выполнению задач, для которых истекло время ожидания. Этот метод вызывается первым для подготовки обработки
        /// </summary>
        public void Update()
        {
            _readySchedulers.Clear();
            long currentTimeMs = Time.Milliseconds;

            if (currentTimeMs < Volatile.Read(ref _nextWakeUpTimeMs))
            {
                return;
            }

            lock (_timedSchedulersLock)
            {
                long nextWakeUpTimeMs = long.MaxValue;

                foreach (var schedulerEntry in _scheduledWakeUpTimeByScheduler)
                {
                    if (schedulerEntry.Value <= currentTimeMs)
                    {
                        _readySchedulers.Add(schedulerEntry.Key);
                        _schedulersToRemove.Add(schedulerEntry.Key);
                    }
                    else if (schedulerEntry.Value < nextWakeUpTimeMs)
                    {
                        nextWakeUpTimeMs = schedulerEntry.Value;
                    }
                }

                for (int i = 0; i < _schedulersToRemove.Count; i++)
                {
                    _scheduledWakeUpTimeByScheduler.Remove(_schedulersToRemove[i]);
                }
                _schedulersToRemove.Clear();

                Volatile.Write(ref _nextWakeUpTimeMs, nextWakeUpTimeMs);
            }
        }

        /// <summary>
        /// Здесб выполняется обработка подготовленных задач. Этот метод вызывается после Update() в нескольких потоках, threadIndex - номер текущего потока, totalThreads - общее количество потоков
        /// </summary>
        /// <param name="threadIndex"></param>
        /// <param name="totalThreads"></param>
        public void Update(int threadIndex, int totalThreads)
        {
            for (int schedulerIndex = threadIndex; schedulerIndex < _readySchedulers.Count; schedulerIndex += totalThreads)
            {
                _readySchedulers[schedulerIndex].WorkerLoop();
            }
        }

        internal void Pulse(TimedTaskScheduler timedTaskScheduler)
        {
            long currentTimeMs = Time.Milliseconds;

            lock (_timedSchedulersLock)
            {
                _scheduledWakeUpTimeByScheduler[timedTaskScheduler] = currentTimeMs;
                Volatile.Write(ref _nextWakeUpTimeMs, currentTimeMs);
            }
        }

        internal void RemovePulse(TimedTaskScheduler timedTaskScheduler)
        {
            lock (_timedSchedulersLock)
            {
                _scheduledWakeUpTimeByScheduler.Remove(timedTaskScheduler);
            }
        }

        internal void Wait(TimedTaskScheduler timedTaskScheduler, int waitMs)
        {
            int nonNegativeWaitMs = Math.Max(0, waitMs);
            long wakeUpTimeMs = Time.Milliseconds + nonNegativeWaitMs;

            lock (_timedSchedulersLock)
            {
                _scheduledWakeUpTimeByScheduler[timedTaskScheduler] = wakeUpTimeMs;

                if (wakeUpTimeMs < Volatile.Read(ref _nextWakeUpTimeMs))
                {
                    Volatile.Write(ref _nextWakeUpTimeMs, wakeUpTimeMs);
                }
            }
        }
    }
}
