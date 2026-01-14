using NetworkGameEngine.JobsSystem;

namespace NetworkGameEngine
{
    public interface IReactCommandWithResultAsync<T, TResult> where T : ICommand
    {
        Job<TResult> ReactCommandAsync(T command);
    }
}
