namespace NetworkGameEngine
{
    public struct CommandResult<T>
    {
        public bool IsFailed { get; private set; }
        public T Result { get; private set; }

        public CommandResult(bool isFailed, T result)
        {
            IsFailed = isFailed;
            Result = result;
        }
    }
}
