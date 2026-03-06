namespace NetworkGameEngine.Interfaces
{
    public interface IMainThreadUpdatableService
    {
        virtual int Priority => 0;
        void Update();
    }
}
