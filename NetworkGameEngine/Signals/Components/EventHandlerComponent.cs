using NetworkGameEngine.Signals.Commands;

namespace NetworkGameEngine.Signals.Components
{
    internal class EventHandlerComponent : Component, IReactCommand<ExecuteActionCommand>
    {
        public void ReactCommand(ref ExecuteActionCommand command)
        {
            command.Execute();
        }
    }
}
