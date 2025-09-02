using NetworkGameEngine.UnitTests;

namespace NetworkGameEngine.Debugger
{
    public interface TestMarkData { }
    public struct TestData : TestMarkData
    {
       public int test;
    }
    public class ReadDataComponent : Component, IReadData<TestData>
    {
        public void UpdateData(ref TestData data)
        {
            data.test = 1;
        }
    }
    internal class TestReadData : WorldTestBase
    {
        [Test]
        public async Task Test1()
        {
            GameObject gameObject = new GameObject();
            gameObject.AddComponent<ReadDataComponent>();

            int objID = await World.AddGameObject(gameObject);

            Thread.Sleep(200);

            TestData d = new TestData();

            Assert.IsTrue(d is TestMarkData md);

            List<TestMarkData> datas = gameObject.ReadAllData<TestMarkData>();
            Assert.IsTrue(datas.Count >= 1);
            foreach(var data in datas)
            {
                Assert.IsTrue(data is TestData);
                Assert.IsTrue(((TestData)data).test == 1);
            }

            gameObject.ReadData(out TestData data1);
            Assert.IsTrue(data1.test == 1);
        }
    }
}
