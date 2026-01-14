using NetworkGameEngine;
using NetworkGameEngine.Generator.Debug;
using NetworkGameEngine.JobsSystem;

namespace SomeSpaceName
{
    public class TestComponent : Component, IReactCommandWithResultAsync<TestRetAsyncCommand, bool>,
                                            IReactCommandWithResult<TestRetCommand, bool>,
                                            IReactCommand<TestNoRetCommand>
    {
        private int v;

        public TestComponent(int v)
        {
            this.v = v;
        }

        public bool ReactCommand(ref TestRetCommand command)
        {
            return true;
        }

        public void ReactCommand(ref TestNoRetCommand command)
        {
            return;
        }

        //------------------------------------------------------

        public async Job<bool> ReactCommandAsync(TestRetAsyncCommand command)
        {
            await Job.Delay(100);
            return true;
        }
    }
}