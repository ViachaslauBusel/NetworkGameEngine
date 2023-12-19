namespace NetworkGameEngine
{
    public interface IReactCommand<T> where T: ICommand
    {
        void ReactCommand(T command);
    }
    //public interface IReactCommand
    //{
    //    void ReactCommand<T>(T command);
    //}
}