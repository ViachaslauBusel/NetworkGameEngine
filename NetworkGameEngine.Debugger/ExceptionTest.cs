using NetworkGameEngine.JobsSystem;
using System.Threading.Tasks;

namespace NetworkGameEngine.UnitTests
{
    public class ExceptionTest : WorldTestBase
    {
        public abstract class TestComponentBase : Component
        {
            protected int _value;
            protected bool _isTested = false;

            public bool IsTested => _isTested;

            protected TestComponentBase(int value)
            {
                _value = value;
            }

            public override async void Update()
            {
                await Job.Wait(Task.Run(() =>
                {
                    _isTested = false;
                    if (_value % 2 == 0)
                    {
                        throw new Exception("Test exception");
                    }
                    _isTested = true;
                }));
            }

            public override async void LateUpdate()
            {
                await Job.Delay(10);
                    if (_value % 2 == 0)
                    {
                        throw new Exception("Test exception");
                    }
                    _isTested = true;
            }
        }

        public class TestComponent1 : TestComponentBase
        {
            public TestComponent1() : base(1) { }
        }

        public class TestComponent2 : TestComponentBase
        {
            public TestComponent2() : base(2) { }
        }

        public class TestComponent3 : TestComponentBase
        {
            public TestComponent3() : base(3) { }
        }

        [Test]
        public void Test1()
        {
            GameObject obj_0 = new GameObject();
            GameObject obj_1 = new GameObject();
            GameObject obj_2 = new GameObject();

            var component_0 = new TestComponent1();
            var component_1 = new TestComponent2();
            var component_2 = new TestComponent3();

            obj_0.AddComponent(component_0);
            obj_1.AddComponent(component_1);
            obj_2.AddComponent(component_2);

            World.AddGameObject(obj_0).Wait();
            World.AddGameObject(obj_1).Wait();
            World.AddGameObject(obj_2).Wait();

            Thread.Sleep(2_000);

            Assert.IsTrue(component_0.IsTested);
            Assert.IsFalse(component_1.IsTested);
            Assert.IsTrue(component_2.IsTested);
        }
    }
}
