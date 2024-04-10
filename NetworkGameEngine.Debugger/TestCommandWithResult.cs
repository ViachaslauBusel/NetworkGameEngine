using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.UnitTests
{
    internal class TestCommandWithResult
    {
        public class TestCMD_0 : ICommand
        {
            public string testValue = "Hello World_0!!!";
        }

        public class TestComponent : Component, IReactCommandWithResult<TestCMD_0, int>
        {

            public int ReactCommand(ref TestCMD_0 command)
            {
                Console.WriteLine(command.testValue);
                return 1;
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
        public void TestCommandWithResultM()
        {
            TestCommandWithResultAsync();
        }
        public async void TestCommandWithResultAsync()
        {
            GameObject go = new GameObject();
            go.AddComponent<TestComponent>();
            TestCMD_0 cmd = new TestCMD_0();
            int result = await go.SendCommandAndReturnResult<TestCMD_0, int>(cmd);

            Assert.AreEqual(1, result);
        }
    }
}
