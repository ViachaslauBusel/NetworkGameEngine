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
            m_world.Init(10, 10, container);
            AfterSetUpWorld();
        }

        protected virtual void BeforeSetUpWorld(out IContainer container)
        {
            container = null;
        }

        protected virtual void AfterSetUpWorld()
        {
        }
    }
}
