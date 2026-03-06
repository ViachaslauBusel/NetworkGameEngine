using NetworkGameEngine.Sync;

namespace NetworkGameEngine.Tools
{
    internal class TimedTask : IDisposable
    {
        //Запускается когда endTime наступает, выполняя действие action.
        // Это нужно для того, чтобы action выполнилась в нужном потоке
        private readonly PriorityEvent m_event;
        private readonly Func<DateTime> m_sheduledTime;
        private readonly object m_source;

        private readonly Action? m_onMarkedDirtyHandler;
        private bool m_disposed;

        public Func<DateTime> EndTime => m_sheduledTime;
        public object Source => m_source;

        public TimedTask(Func<DateTime> endTime, object source, Action action, Action onMarkedDirtyHandler)
        {
            m_sheduledTime = endTime;
            m_source = source;
            m_event = new PriorityEvent();
            m_event.Subscribe(action);

            if (source is SyncMarkers marker)
            {
                m_onMarkedDirtyHandler = onMarkedDirtyHandler;
                marker.OnMarkedDirty.Subscribe(m_onMarkedDirtyHandler);
            }
        }

        public void Execute()
        {
            m_event.Invoke();
        }

        public void Dispose()
        {
            if (m_disposed) return;
            m_disposed = true;

            if (m_source is SyncMarkers marker && m_onMarkedDirtyHandler != null)
            {
                marker.OnMarkedDirty.Unsubscribe(m_onMarkedDirtyHandler);
            }
        }
    }
}
