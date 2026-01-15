using NetworkGameEngine.JobsSystem;

namespace NetworkGameEngine.UnitTests
{
    public struct ThreadAffinityProbeCommand : ICommand
    {
    }
    /// <summary>
    /// Verifies that an asynchronous command with a result preserves thread affinity.
    /// This means:
    /// - The code inside the command-reactor component executes on the same thread where the component was created.
    /// - After the command completes, the calling component continues on the same thread where the command was invoked.
    /// </summary>
    internal class AsyncCommandWithResultThreadAffinityTests : WorldTestBase
    {
        public sealed class ThreadAffinityProbeResponderComponent : Component, IReactCommandWithResultAsync<ThreadAffinityProbeCommand, bool>
        {
            private int _initialThreadId;

            public int InitialThreadId => _initialThreadId;

            public override void Init()
            {
                _initialThreadId = Environment.CurrentManagedThreadId;
            }

            public async Job<bool> ReactCommandAsync(ThreadAffinityProbeCommand command)
            {
                var isThreadAffinityPreserved = IsOnInitialThread();

                await Job.Delay(10);
                isThreadAffinityPreserved &= IsOnInitialThread();

                await Job.Run(() => Thread.Sleep(10));
                isThreadAffinityPreserved &= IsOnInitialThread();

                return isThreadAffinityPreserved;
            }

            private bool IsOnInitialThread() => _initialThreadId == Environment.CurrentManagedThreadId;
        }

        public sealed class ThreadAffinityProbeRequesterComponent : Component
        {
            private readonly GameObject _receiver;
            private int _requesterThreadId;

            public bool Passed { get; private set; }
            public int RequesterThreadId => _requesterThreadId;

            public ThreadAffinityProbeRequesterComponent(GameObject receiver)
            {
                _receiver = receiver;
            }

            public override async void Start()
            {
                _requesterThreadId = Environment.CurrentManagedThreadId;

                var result = await _receiver.ThreadAffinityProbeAsync();

                Passed =
                    result.IsSuccess &&
                    result.Result &&
                    _requesterThreadId == Environment.CurrentManagedThreadId;
            }
        }

        [Test]
        public void Async_command_with_result_preserves_thread_affinity()
        {
            RunAsyncCommandWithResultThreadAffinityProbe();
        }

        public async void RunAsyncCommandWithResultThreadAffinityProbe()
        {
            var receiver = new GameObject();
            var receiverComponent = new ThreadAffinityProbeResponderComponent();
            receiver.AddComponent(receiverComponent);

            var requester = new GameObject();
            var requesterComponent = new ThreadAffinityProbeRequesterComponent(receiver);
            requester.AddComponent(requesterComponent);

            World.AddGameObject(receiver);
            World.AddGameObject(requester);

            int maxWaitIterations = 500;

            while (!requesterComponent.Passed && maxWaitIterations-- > 0)
            {
                Thread.Sleep(1);
            }

            Assert.AreEqual(true, requesterComponent.Passed);
            Assert.AreNotEqual(receiverComponent.InitialThreadId, requesterComponent.RequesterThreadId);
        }
    }
}