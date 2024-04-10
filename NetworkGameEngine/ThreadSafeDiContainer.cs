using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Zenject;

namespace NetworkGameEngine
{
    public struct DiContainerObject : IDisposable
    {
        private DiContainer m_container;
        private object m_locker;

        public DiContainer Container => m_container;

        public DiContainerObject(DiContainer container, Object locker)
        {
            m_container = container;
            m_locker = locker;
        }

        public void Dispose()
        {
            Monitor.Exit(m_locker);
        }
    }

    internal class ThreadSafeDiContainer
    {
        private DiContainer m_container;
        private object m_locker;

        public ThreadSafeDiContainer()
        {
            m_container = new DiContainer();
            m_locker = new object();
        }

        public DiContainerObject LockContainer()
        {
            Monitor.Enter(m_locker);
            return new DiContainerObject(m_container, m_locker);
        }
    }
}
