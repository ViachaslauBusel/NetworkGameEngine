using NetworkGameEngine;
using NUnit.Framework.Internal.Commands;
using Zenject;

namespace Test
{

    public interface ITestService 
    { 
        public string Name { get; set; }
    }
    public class TestService : ITestService
    {
        public string Name { get; set; }
    }
    public class TestCommand : ICommand
{

}
    public class TestComponent : Component, IReactCommand<TestCommand>
    {
        public volatile int m_inject = 0;
        public volatile int m_init = 0;
        public volatile int m_start = 0;
        public volatile int m_update = 0;
        public volatile int m_lateUpdate = 0;
        public volatile int m_cmd = 0;
        public volatile int m_destroy = 0;


        [Inject]
        public void InjectServices(ITestService testService)
        {
            if (testService.Name.Equals("TruePass")) m_inject++;
            Console.WriteLine($"Hello from Inject:{Thread.CurrentThread.ManagedThreadId}, {testService.Name}");
        }
        public override async Task Init()
        {
            m_init++;
            Console.WriteLine($"Hello from Init:{Thread.CurrentThread.ManagedThreadId}");
        }

        public override void Start()
        {
        m_start++;
            Console.WriteLine($"Hello from Start:{Thread.CurrentThread.ManagedThreadId}");
        }

        public override void Update()
        {
        m_update++;
            Console.WriteLine($"Hello from Update:{Thread.CurrentThread.ManagedThreadId}");
        }

        public override void LateUpdate()
        {
            m_lateUpdate++;
            Console.WriteLine($"Hello from LateUpdate:{Thread.CurrentThread.ManagedThreadId}");
        }

        public override void OnDestroy()
        {
            m_destroy++;
            Console.WriteLine($"Hello from OnDestroy:{Thread.CurrentThread.ManagedThreadId}");
        }

        public void ReactCommand(TestCommand command)
        {
            m_cmd++;
            Console.WriteLine($"Hello from ReactTestCommand:{Thread.CurrentThread.ManagedThreadId}");
        }
    }
    public class TestWorld
    {
        private World m_world;

        [SetUp]
        public void Setup()
        {
            TestService testService = new TestService()
            { Name= "TruePass" };

            m_world = new World();
            m_world.RegisterService<ITestService>(testService);
            m_world.Init(8);
            Thread.Sleep(100);
            Thread th = new Thread(WorldThread);
            th.Start();
        }

        private void WorldThread()
        {
         
            while (true)
            {
                m_world.Update();
                Thread.Sleep(100);
            }
        }

        [Test]
        public async Task Test1()
        {
            GameObject testObj = new GameObject();
            TestComponent testComponent = new TestComponent();
            testObj.AddComponent(testComponent);  

            int id = await m_world.AddGameObject(testObj);

            Thread.Sleep(1_000);
            testObj.SendCommand(new TestCommand());
            Thread.Sleep(1_000);
            m_world.RemoveGameObject(id);
            Thread.Sleep(1_000);
            Assert.IsTrue(testComponent.m_init == 1);
            Assert.IsTrue(testComponent.m_start== 1);
            Assert.IsTrue(testComponent.m_cmd == 1);
            Assert.IsTrue(testComponent.m_destroy == 1);
            Assert.IsTrue(testComponent.m_lateUpdate == testComponent.m_update);

            Console.WriteLine($"Update:{testComponent.m_update}");
            Assert.Pass();
        }
    }
}