namespace NetworkGameEngine
{
    public interface IReactCommandWithResult<T, TResult> where T : ICommand
    {
        TResult ReactCommand(ref T command);
    }
}
