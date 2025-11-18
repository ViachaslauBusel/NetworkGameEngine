using NetworkGameEngine.UnitTests;

namespace NetworkGameEngine.Debugger
{
    public class TestCMD_0 : ICommand
    {
        public string testValue = "Hello World_0!!!";
    }
    public class TestCMD_1 : ICommand
    {
        public string testValue = "Hello World_1!!!";
    }
    public struct TestData_0 
    {
        public int OutValue;
    }
    public class TestComponent : Component, IReactCommand<TestCMD_0>, IReactCommand<TestCMD_1>
    {

        public void ReactCommand(ref TestCMD_0 command)
        {
            Console.WriteLine(command.testValue);
        }

        public void ReactCommand(ref TestCMD_1 command)
        {
            Console.WriteLine(command.testValue);
        }

        public void UpdateData(ref TestData_0 data)
        {
            data.OutValue = 1;
        }
    }
    public class TestCommand : WorldTestBase
    {
        
        [Test]
        public async Task Test1()
        {
            GameObject gameObject = new GameObject();
            gameObject.AddComponent<TestComponent>();
          //  gameObject.AddComponent<TestInternalComponent>();
            uint objID = await World.AddGameObject(gameObject);
            Thread.Sleep(150);
            gameObject.SendCommand(new TestCMD_0());
            gameObject.SendCommand(new TestCMD_1());

            //gameObject.ReadData(out TestData_0 data);
          
            Thread.Sleep(800);
            //Assert.IsTrue(data.OutValue == 1);
        }
    }
}
