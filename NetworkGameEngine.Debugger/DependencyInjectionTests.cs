using Autofac;

namespace NetworkGameEngine.UnitTests
{
    internal class DependencyInjectionTests : WorldTestBase
    {
        public class SomeService
        {
            private string _value;

            public string Value => _value;

            internal void SetValue(string v)
            {
                _value = v;
            }
        }

        public class TestPublicComponent : Component
        {
            private SomeService _service;

            public SomeService Service => _service;

            [Inject]
            public void Inject(SomeService service)
            {
                _service = service;
            }
        }

        public class TestPrivateComponent : Component
        {
            private SomeService _service;

            public SomeService Service => _service;

            [Inject]
            private void Inject(SomeService service)
            {
                _service = service;
            }
        }

        public class TestBaseComponent : TestPrivateComponent
        {
            private SomeService _secondService;

            public SomeService SecondService => _secondService;

            [Inject]
            protected void Inject(SomeService service)
            {
                _secondService = service;
            }
        }

        protected override void BeforeSetUpWorld(out IContainer container)
        {
            var builder = new ContainerBuilder();
            builder.RegisterType<SomeService>();
            container = builder.Build();

            container.Resolve<SomeService>().SetValue("TestValue");
        }

        [Test]
        public void TestPublicInjection()
        {
            GameObject obj = new GameObject();
            TestPublicComponent publicComponent = new TestPublicComponent();
            obj.AddComponent(publicComponent);
            World.AddGameObject(obj).Wait();

            Thread.Sleep(200);

            Assert.IsNotNull(publicComponent.Service);
        }

        [Test]
        public void TestPrivateInjection()
        {
            GameObject obj = new GameObject();
            TestPrivateComponent privateComponent = new TestPrivateComponent();
            obj.AddComponent(privateComponent);
            World.AddGameObject(obj).Wait();
            Thread.Sleep(800);
            Assert.IsNotNull(privateComponent.Service.Value.Equals("TestValue"));
        }

        [Test]
        public void TestBaseInjection()
        {
            GameObject obj = new GameObject();
            TestBaseComponent baseComponent = new TestBaseComponent();
            obj.AddComponent(baseComponent);
            World.AddGameObject(obj).Wait();
            Thread.Sleep(200);
            Assert.IsNotNull(baseComponent.Service);
            Assert.IsNotNull(baseComponent.SecondService);
        }
    }
}
