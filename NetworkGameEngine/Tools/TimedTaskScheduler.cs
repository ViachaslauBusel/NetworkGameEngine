namespace NetworkGameEngine.Tools
{
    public class TimedTaskScheduler : IDisposable
    {
        private readonly MainThreadDelayDispatcher _mainThreadDelayDispatcher;
        private readonly object m_lock = new object();
        private readonly List<TimedTask> m_tasks = new List<TimedTask>();
        private bool m_isWorking = true;

        internal TimedTaskScheduler(MainThreadDelayDispatcher mainThreadDelayDispatcher)
        {
            _mainThreadDelayDispatcher = mainThreadDelayDispatcher;
        }

        public void SheduleTask(Func<DateTime> endTime, object source, Action action)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (action == null) throw new ArgumentNullException(nameof(action));
            if (endTime == null) throw new ArgumentNullException(nameof(endTime));

            TimedTask task = new TimedTask(endTime, source, action, SortTaskSafe);

            lock (m_lock)
            {
                InsertSortedUnsafe(task);
            }

            _mainThreadDelayDispatcher.Pulse(this);
        }

        public void RemoveTaskBySource(object source)
        {
            if (source == null) return;

            List<TimedTask> removedTasks = new List<TimedTask>();
            bool hasTasksAfterRemoval;

            lock (m_lock)
            {
                for (int i = m_tasks.Count - 1; i >= 0; i--)
                {
                    if (ReferenceEquals(m_tasks[i].Source, source))
                    {
                        removedTasks.Add(m_tasks[i]);
                        m_tasks.RemoveAt(i);
                    }
                }

                hasTasksAfterRemoval = m_tasks.Count > 0;
            }

            if (hasTasksAfterRemoval)
                _mainThreadDelayDispatcher.Pulse(this);
            else
                _mainThreadDelayDispatcher.RemovePulse(this);

            foreach (var task in removedTasks)
            {
                task.Dispose();
            }
        }

        private void Stop()
        {
            List<TimedTask> pendingTasks = new List<TimedTask>();

            lock (m_lock)
            {
                if (!m_isWorking) return;
                m_isWorking = false;

                pendingTasks.AddRange(m_tasks);
                m_tasks.Clear();
            }

            _mainThreadDelayDispatcher.RemovePulse(this);

            foreach (var task in pendingTasks)
            {
                task.Dispose();
            }
        }

        public void Dispose()
        {
            Stop();
        }

        internal void WorkerLoop()
        {
            TimedTask? taskToExecute = null;

            lock (m_lock)
            {
                if (!m_isWorking)
                    return;

                if (m_tasks.Count == 0)
                {
                    _mainThreadDelayDispatcher.RemovePulse(this);
                    return;
                }

                var nextTask = m_tasks[0];
                var wait = ResolveEndTime(nextTask) - DateTime.Now;

                if (wait <= TimeSpan.Zero)
                {
                    taskToExecute = nextTask;
                    m_tasks.RemoveAt(0);
                }
                else
                {
                    int waitMs = (int)Math.Min(wait.TotalMilliseconds, int.MaxValue);
                    _mainThreadDelayDispatcher.Wait(this, waitMs);
                    return;
                }
            }

            try
            {
                // Сам Action будет выполнен в другом потоке
                taskToExecute.Execute();
            }
            catch
            {
                // Intentionally ignored to keep scheduler thread alive.
            }
            finally
            {
                taskToExecute.Dispose();
            }

            lock (m_lock)
            {
                if (!m_isWorking || m_tasks.Count == 0)
                    _mainThreadDelayDispatcher.RemovePulse(this);
                else
                    _mainThreadDelayDispatcher.Pulse(this);
            }
        }

        private void InsertSortedUnsafe(TimedTask task)
        {
            if (!m_isWorking) return;

            int index = m_tasks.FindIndex(t => ResolveEndTime(t) > ResolveEndTime(task));
            if (index < 0)
                m_tasks.Add(task);
            else
                m_tasks.Insert(index, task);
        }

        private void SortTaskSafe()
        {
            bool hasTasks;

            lock (m_lock)
            {
                SortTasksUnsafe();
                hasTasks = m_tasks.Count > 0;
            }

            if (hasTasks)
                _mainThreadDelayDispatcher.Pulse(this);
            else
                _mainThreadDelayDispatcher.RemovePulse(this);
        }

        private void SortTasksUnsafe()
        {
            m_tasks.Sort((a, b) => ResolveEndTime(a).CompareTo(ResolveEndTime(b)));
        }

        private static DateTime ResolveEndTime(TimedTask task)
        {
            try
            {
                return task.EndTime.Invoke();
            }
            catch
            {
                return DateTime.MaxValue;
            }
        }
    }
}
