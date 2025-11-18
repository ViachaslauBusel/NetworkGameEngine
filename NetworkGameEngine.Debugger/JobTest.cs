using NetworkGameEngine.JobsSystem;
using System.Diagnostics;

namespace NetworkGameEngine.UnitTests
{
    public class JobTestComponent : Component
    {
        public int Result { get; private set; } = 0;
        public int Result2 { get; private set; } = 0;
        public override void Init()
        {
            //Stopwatch stopwatch = Stopwatch.StartNew();
            //await Job.Wait(Task.Delay(500));
            //stopwatch.Stop();
            //Result = (int)stopwatch.ElapsedMilliseconds;

            Stopwatch stopwatch = Stopwatch.StartNew();
            TestWait();
            stopwatch.Stop();
            Result = (int)stopwatch.ElapsedMilliseconds;
        }

        public async void TestWait()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            await Job.Wait(Task.Delay(500));
            stopwatch.Stop();
            Result2 = (int)stopwatch.ElapsedMilliseconds;
        }
    }
    public class JobTest : WorldTestBase
    {
        [Test]
        public void Test_Wait_Task()
        {
            GameObject obj = new GameObject();
            var jobTestComponent = new JobTestComponent();
            obj.AddComponent(jobTestComponent);
            World.AddGameObject(obj);
            Thread.Sleep(50);

            Assert.LessOrEqual(1, jobTestComponent.Result);
            Assert.GreaterOrEqual(500, jobTestComponent.Result);
        }
    }
}
