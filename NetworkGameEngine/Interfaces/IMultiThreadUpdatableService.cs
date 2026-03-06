namespace NetworkGameEngine.Interfaces
{
    public interface IMultiThreadUpdatableService
    {
        virtual int Priority => 0;

        /// <summary>
        /// Called for each worker thread during the MultiThreadService stage.
        /// </summary>
        /// <param name="threadIndex">The index of the current thread.</param>
        /// <param name="totalThreads">The total number of worker threads.</param>
        void Update(int threadIndex, int totalThreads);
    }
}
