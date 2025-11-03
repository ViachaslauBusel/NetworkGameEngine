using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.JobsSystem
{
    // Перехватывает исключения из async void продолжений, выполняющихся на потоке мира
    public sealed class EngineSynchronizationContext : SynchronizationContext
    {
        private readonly World _world;

        public EngineSynchronizationContext(World world)
        {
            _world = world;
        }

        public override void Post(SendOrPostCallback d, object state)
        {
            ThreadPool.QueueUserWorkItem(static s =>
            {
                var (callback, cbState, world) = ((SendOrPostCallback, object, World))s!;
                try
                {
                    callback(cbState);
                }
                catch (Exception ex)
                {
                    world.LogError($"Unhandled async exception: {ex}");
                }
            }, (d, state, _world));
        }

        public override void Send(SendOrPostCallback d, object state)
        {
            try
            {
                d(state);
            }
            catch (Exception ex)
            {
                _world.LogError($"Unhandled async exception: {ex}");
            }
        }

        public override SynchronizationContext CreateCopy() => new EngineSynchronizationContext(_world);
    }
}
