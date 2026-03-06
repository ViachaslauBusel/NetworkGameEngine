using NetworkGameEngine.Sync;
using NetworkGameEngine.Tools;
using System.Threading;

namespace NetworkGameEngine.UnitTests
{
    public class TimedTaskSchedulerTests : WorldTestBase
    {
        public class TimeContainer : SyncMarkers
        {
            private DateTime _currentTime;
            public DateTime CurrentTime => _currentTime;

            public TimeContainer(DateTime currentTime)
            {
                _currentTime = currentTime;
            }

            public void AddSeconds(double seconds)
            {
                _currentTime = _currentTime.AddSeconds(seconds);
                MakeAllDirty();
            }
        }

        [Test]
        public void SheduleTask_ExecutesAction_WhenTimeIsReached()
        {
            TimedTaskScheduler scheduler = World.Resolve<TimedTaskScheduler>();
            Assert.That(scheduler, Is.Not.Null);

            using ManualResetEventSlim actionExecuted = new ManualResetEventSlim(false);
            object taskSource = new object();
            int executionCount = 0;

            DateTime executeAt = DateTime.Now.AddMilliseconds(80);

            scheduler.SheduleTask(
                () => executeAt,
                taskSource,
                () =>
                {
                    Interlocked.Increment(ref executionCount);
                    actionExecuted.Set();
                });

            bool wasExecuted = actionExecuted.Wait(TimeSpan.FromSeconds(2));

            Assert.That(wasExecuted, Is.True, "Scheduled action was not executed in time.");
            Assert.That(executionCount, Is.EqualTo(1), "Scheduled action must be executed exactly once.");
        }

        [Test]
        public void RemoveTaskBySource_PreventsExecution_ForRemovedSource()
        {
            TimedTaskScheduler scheduler = World.Resolve<TimedTaskScheduler>();
            Assert.That(scheduler, Is.Not.Null);

            using ManualResetEventSlim actionExecuted = new ManualResetEventSlim(false);
            object taskSource = new object();

            DateTime executeAt = DateTime.Now.AddMilliseconds(500);

            scheduler.SheduleTask(
                () => executeAt,
                taskSource,
                () => actionExecuted.Set());

            scheduler.RemoveTaskBySource(taskSource);

            bool wasExecuted = actionExecuted.Wait(TimeSpan.FromMilliseconds(800));

            Assert.That(wasExecuted, Is.False, "Removed task should not be executed.");
        }

        [Test]
        public void SheduleTask_ExecutesAfterTimeSourceChanges_AndMarksDirty()
        {
            TimedTaskScheduler scheduler = World.Resolve<TimedTaskScheduler>();
            Assert.That(scheduler, Is.Not.Null);

            using ManualResetEventSlim actionExecuted = new ManualResetEventSlim(false);
            TimeContainer timeContainer = new TimeContainer(DateTime.Now.AddSeconds(10)); // далеко в будущем

            scheduler.SheduleTask(
                () => timeContainer.CurrentTime,
                timeContainer,
                () => actionExecuted.Set());

            // До изменения времени задача не должна выполниться быстро
            bool executedBeforeTimeChange = actionExecuted.Wait(TimeSpan.FromMilliseconds(200));
            Assert.That(executedBeforeTimeChange, Is.False, "Task should not execute before time source change.");

            // Сдвигаем время источника в прошлое + MakeAllDirty внутри AddSeconds
            timeContainer.AddSeconds(-20);

            bool executedAfterTimeChange = actionExecuted.Wait(TimeSpan.FromSeconds(2));
            Assert.That(executedAfterTimeChange, Is.True, "Task should execute after time source changed and marked dirty.");
        }
    }
}
