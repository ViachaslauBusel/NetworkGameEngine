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
            var result = await go.SendCommandAndReturnResult<TestCMD_0, int>(cmd);

            Assert.AreEqual(1, result.Result);
        }
    }
}
