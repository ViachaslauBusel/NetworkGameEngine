using NetworkGameEngine.JobsSystem;
using System.Collections.Concurrent;

namespace NetworkGameEngine
{
    internal class CommandContainerWithResut<T, TResult> : CommandContainer where T : ICommand
    {
        private T m_command;
        private TResult m_result;
        private volatile bool m_isCompleted = false;
        private volatile bool m_isCanceled = false;

        public bool IsCompleted => m_isCompleted;
        public bool IsCanceled => m_isCanceled;
        public TResult Result => m_result;

        internal CommandContainerWithResut(T command)
        {
            m_command = command;
        }

        internal override Type GetCommandType() => typeof(T);

        internal override void Invoke(object cmdListener)
        {
            if (m_isCanceled)
            {
                m_isCompleted = true;
                return;
            }
            IReactCommandWithResult<T, TResult> reactCommand = cmdListener as IReactCommandWithResult<T, TResult>;
            m_result = reactCommand.ReactCommand(ref m_command);
            m_isCompleted = true;
        }
        internal void Cancel()
        {
            m_isCanceled = true;
        }
    }

    internal class CommandContainer<T> : CommandContainer where T : ICommand
    {
        private T m_command;

        public CommandContainer(T command)
        {
            m_command = command;
        }

        internal override Type GetCommandType() => typeof(T);

        internal override void Invoke(object cmdListener)
        {
            IReactCommand<T> reactCommand = cmdListener as IReactCommand<T>;
            reactCommand.ReactCommand(ref m_command);
        }
    }

    internal abstract class CommandContainer
    {
        internal abstract void Invoke(object cmdListener);

        internal abstract Type GetCommandType();
    }

    public sealed partial class GameObject
    {
        private Dictionary<Type, List<object>> m_commandListener = new Dictionary<Type, List<object>>();
        private ConcurrentQueue<CommandContainer> m_commands = new ConcurrentQueue<CommandContainer>();

        internal void AddListener(Type cmdType, object reactCommand)
        {
            GetCommandListener(cmdType).Add(reactCommand);
        }

        internal void RemoveListener(Type cmdType, object reactCommand)
        {
            GetCommandListener(cmdType).Remove(reactCommand);
        }

        private List<object> GetCommandListener(Type type)
        {
            if(!m_commandListener.ContainsKey(type)) 
            {
                m_commandListener.Add(type, new List<object>());
            }

            return m_commandListener[type];
        }

        //
        internal void CallCommand()
        {
            while (m_commands.TryDequeue(out CommandContainer cmd))
            {
                foreach (var cmdListener in GetCommandListener(cmd.GetCommandType()))
                {
                    cmd.Invoke(cmdListener);
                }
            }
        }

        //PUBLIC part >>>
        public void SendCommand<T>(T command) where T : ICommand 
        {
            m_commands.Enqueue(new CommandContainer<T>(command));
        }

        public async Job<CommandResult<TResult>> SendCommandAndReturnResult<T, TResult>(T command, int waitTime = 0) where T : ICommand
        {
           var commandContainer = new CommandContainerWithResut<T, TResult>(command);
            m_commands.Enqueue(commandContainer);

            long endWaitTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() + waitTime;
            await new WaitUntilJob(() => commandContainer.IsCompleted || commandContainer.IsCanceled 
                                      || endWaitTime < DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            if (commandContainer.IsCompleted == false)
            {
                commandContainer.Cancel();
                return new CommandResult<TResult>(false, default);
            }

            return new CommandResult<TResult>(true, commandContainer.Result);
        }
    }
}
