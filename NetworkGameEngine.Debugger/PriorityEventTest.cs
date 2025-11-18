namespace NetworkGameEngine.UnitTests
{
    public class PriorityEventTest
    {
        [Test]
        public void TestPriorityEvent()
        {
            var pe = new PriorityEvent<string,int>();
            pe.Subscribe(SomeMethod10, 10);
            pe.Subscribe(SomeMethod5, 5);
            pe.Invoke("Test", 1);
        }

        private void SomeMethod5(string arg1, int arg2)
        {
            throw new NotImplementedException();
        }

        private void SomeMethod10(string message, int someInt)
        {
            Console.WriteLine($"Handler with priority 10 received: {message}");
        }
    }
}
