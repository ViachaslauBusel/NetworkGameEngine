namespace NetworkGameEngine
{
    public struct CommandResult<T>
    {
        private readonly T m_result;
        private readonly bool m_isSuccess;

        public bool IsSuccess => m_isSuccess;
        public bool IsFailed => !m_isSuccess;
        public T Result => m_result;


        public CommandResult(bool isSuccess, T result)
        {
            m_isSuccess = isSuccess;
            m_result = result;
        }
    }
}
