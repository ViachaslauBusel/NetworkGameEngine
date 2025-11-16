namespace NetworkGameEngine.Signals.Commands
{
    internal struct ExecuteActionCommand : ICommand
    {
        private Action m_handler;

        public ExecuteActionCommand(Action handler)
        {
            m_handler = handler;
        }

        public void Execute()
        {
            m_handler.Invoke();
        }
    }
}