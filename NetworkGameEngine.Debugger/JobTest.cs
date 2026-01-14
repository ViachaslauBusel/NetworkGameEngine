using NetworkGameEngine.JobsSystem;
using System.Diagnostics;
using System.Threading.Tasks;

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
            stopwatch = Stopwatch.StartNew();
            TestWait();
            stopwatch.Stop();
            Result = (int)stopwatch.ElapsedMilliseconds;
        }

        public async Job TestWait()
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            await Job.Run(() => Thread.Sleep(500));
            stopwatch.Stop();
            Result2 = (int)stopwatch.ElapsedMilliseconds;
        }

        private async Task RunTask()
        {
            Thread.Sleep(250);
            await Task.Delay(250);
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
            Thread.Sleep(600);

            Assert.LessOrEqual(jobTestComponent.Result, 1);
            Assert.GreaterOrEqual(jobTestComponent.Result2, 500);
        }
    }
}
