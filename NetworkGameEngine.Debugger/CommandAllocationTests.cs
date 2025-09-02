using System.Diagnostics;

namespace NetworkGameEngine.UnitTests
{

    internal class CommandAllocationTests : WorldTestBase
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


        [Test]
        public void TestCommandAllocation()
        {
            GameObject obj = new GameObject();
            obj.AddComponent<TestComponent>();
            World.AddGameObject(obj).Wait();
           
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
            Assert.IsTrue(memory2 - memory < 10000000);
        }

    }
}
