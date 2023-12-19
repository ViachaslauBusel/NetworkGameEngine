namespace NetworkGameEngine.Debugger
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Test1()
        {
            int int_obj = 1;
            dynamic dynamic_obj = int_obj;
            dynamic_obj = new TestData_0();
            object int_object = 1;
            int a = (int)int_object;
            Test(dynamic_obj);
            Assert.Pass();
        }

        private void Test(int int_obj)
        {
            Console.WriteLine(int_obj);
        }
        private void Test(TestData_0 data)
        {
            Console.WriteLine(data);
        }
    }
}