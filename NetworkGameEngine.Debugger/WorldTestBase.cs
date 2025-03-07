using Autofac;

namespace NetworkGameEngine.UnitTests
{
    public class WorldTestBase
    {
        private World m_world;

        protected World World => m_world;

        [SetUp]
        public void Setup()
        {
            BeforeSetUpWorld(out IContainer container);
            m_world = new World();
            m_world.Init(8, container);
            Thread.Sleep(100);
            Thread th = new Thread(WorldThread);
            th.IsBackground = true;
            th.Start();
            AfterSetUpWorld();
        }

        protected virtual void BeforeSetUpWorld(out IContainer container)
        {
            container = null;
        }

        protected virtual void AfterSetUpWorld()
        {
        }

        private void WorldThread(object? obj)
        {
            while (true)
            {
                m_world.Update();
                Thread.Sleep(100);
            }
        }
    }
}
