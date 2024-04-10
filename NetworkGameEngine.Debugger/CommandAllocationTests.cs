using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.UnitTests
{
    
    internal class CommandAllocationTests
    {

        internal struct TestCommand : ICommand
        {
            public int TestValue;
            public int Value;
            public int Value2;
            public int Value3;
            public int Value4;
        }
        internal class TestComponent : Component, IReactCommand<TestCommand>
        {
            private long m_startTime;
            public void ReactCommand(ref TestCommand command)
            {
                if(command.TestValue == 0)
                {
                    m_startTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
                else if(command.TestValue == 1_000_000)
                {
                    long time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - m_startTime;
                    Console.WriteLine($"Test passed:{time}");
                }
            }
        }

        private World m_world;
       


        [SetUp]
        public void Setup()
        {
            m_world = new World();
            m_world.Init(8);
            Thread.Sleep(100);
            Thread th = new Thread(WorldThread);
            th.IsBackground = true;
            th.Start();
        }

        private void WorldThread(object? obj)
        {
            while (true)
            {
                m_world.Update();
                Thread.Sleep(100);
            }
        }

        [Test]
        public void TestCommandAllocation()
        {
            GameObject obj = new GameObject();
            obj.AddComponent<TestComponent>();
            m_world.AddGameObject(obj).Wait();
           
            long memory = GC.GetTotalMemory(true);
            Stopwatch stopwatch = Stopwatch.StartNew();
          
            for (int i = 0; i <= 1_000_000; i++)
            {
                obj.SendCommand(new TestCommand() { TestValue = i });
            }
           
            stopwatch.Stop();
          
            Thread.Sleep(4_000);
            GC.Collect();
            // Подождать пока соберется мусор
            GC.WaitForPendingFinalizers();
            Thread.Sleep(1_000);
            long memory2 = GC.GetTotalMemory(true);
            long allocated = memory2 - memory;
            Console.WriteLine($"Allocated memory: {allocated}, total time:{stopwatch.ElapsedMilliseconds}");
            Assert.IsTrue(memory2 - memory < 1000000);
        }

    }
}
