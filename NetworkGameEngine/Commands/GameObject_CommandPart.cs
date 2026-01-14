using NetworkGameEngine.JobsSystem;
using System.Collections.Concurrent;

namespace NetworkGameEngine
{
    internal class CommandContainerWithResult<T, TResult> : CommandContainer where T : ICommand
    {
        private T m_command;
        private TResult m_result;
        private Job<TResult> m_resultAsync;
        private volatile bool m_isDispatched = false;
        private volatile bool m_isCanceled = false;

        public bool IsDispatched => m_isDispatched;
        public bool IsCanceled => m_isCanceled;
        public TResult Result => m_result;
        public bool IsAsync => m_resultAsync != null;

        internal CommandContainerWithResult(T command)
        {
            m_command = command;
        }

        internal override Type GetCommandType() => typeof(T);

        internal override void Invoke(object cmdListener)
        {
            lock (this)
            {
                if (m_isCanceled)
                {
                    m_isDispatched = true;
                    return;
                }
                if (cmdListener is IReactCommandWithResult<T, TResult> reactCommand)
                {
                    m_result = reactCommand.ReactCommand(ref m_command);
                    m_isDispatched = true;
                }
                else if (cmdListener is IReactCommandWithResultAsync<T, TResult> reactCommandAsync)
                {
                    m_resultAsync = reactCommandAsync.ReactCommandAsync(m_command);
                    m_isDispatched = true;
                }
                else
                {
                    m_isCanceled = true;
                }
            }
        }

        internal async Job WaitForAsyncResult()
        {
            if (m_resultAsync != null)
            {
                m_result = await m_resultAsync;
            }
        }

        internal bool TryCancel()
        {
            lock (this)
            {
                if (m_isDispatched) return false;
                m_isCanceled = true;
                return true;
            }
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
        private Dictionary<Type, object> m_commandListenerWithResult = new Dictionary<Type, object>();
        private Dictionary<Type, List<object>> m_commandListener = new Dictionary<Type, List<object>>();
        private ConcurrentQueue<CommandContainer> m_commands = new ConcurrentQueue<CommandContainer>();
        private ConcurrentQueue<CommandContainer> m_commandsWithResult = new ConcurrentQueue<CommandContainer>();
        private object m_cmdLockObject = new object();


        private void RegisterCommandListenersForComponent(Component c)
        {
            foreach (var @interface in c.GetType().GetInterfaces().Where(x => x.IsGenericType))
            {
                if (@interface.GetGenericTypeDefinition() == typeof(IReactCommand<>))
                {
                    AddListener(@interface.GenericTypeArguments[0], c);
                }
                if (@interface.GetGenericTypeDefinition() == typeof(IReactCommandWithResult<,>))
                {
                    AddListenerWithResult(@interface.GenericTypeArguments[0], c);
                }
                if (@interface.GetGenericTypeDefinition() == typeof(IReactCommandWithResultAsync<,>))
                {
                    AddListenerWithResult(@interface.GenericTypeArguments[0], c);
                }
            }
        }

        private void UnregisterCommandListenersForComponent(Component c)
        {
            foreach (var @interface in c.GetType().GetInterfaces().Where(x => x.IsGenericType))
            {
                if (@interface.GetGenericTypeDefinition() == typeof(IReactCommand<>))
                {
                    RemoveListener(@interface.GenericTypeArguments[0], c);
                }
                if (@interface.GetGenericTypeDefinition() == typeof(IReactCommandWithResult<,>))
                {
                    RemoveListenerWithResult(@interface.GenericTypeArguments[0]);
                }
                if (@interface.GetGenericTypeDefinition() == typeof(IReactCommandWithResultAsync<,>))
                {
                    RemoveListenerWithResult(@interface.GenericTypeArguments[0]);
                }
            }
        }

        private void AddListenerWithResult(Type cmdType, object reactCommand)
        {
            if (m_commandListenerWithResult.ContainsKey(cmdType))
            {
                m_world.LogError($"GameObject Command listener with result for command {cmdType} is already registered in {reactCommand.GetType()}");
            }
            m_commandListenerWithResult[cmdType] = reactCommand;
        }

        private void RemoveListenerWithResult(Type cmdType)
        {
            m_commandListenerWithResult.Remove(cmdType);
        }

        private object GetCommandListenerWithResult(Type type)
        {
            m_commandListenerWithResult.TryGetValue(type, out var listener);
            return listener;
        }

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

        internal void DispatchPendingCommands()
        {
            while (m_commands.TryDequeue(out CommandContainer cmd))
            {
                foreach (var listener in GetCommandListener(cmd.GetCommandType()))
                {
                    try
                    {
   
                        cmd.Invoke(listener);
                    }
                    catch (Exception ex)
                    {
                        m_world.LogError($"GameObject Command processing error in {listener.GetType()}: {ex}");
                    }
                }
            }

            while (m_commandsWithResult.TryDequeue(out CommandContainer cmd))
            {
                var listener = GetCommandListenerWithResult(cmd.GetCommandType());
                if (listener != null)
                {
                    try
                    {
                        cmd.Invoke(listener);
                    }
                    catch (Exception ex)
                    {
                        m_world.LogError($"GameObject Command processing error in {listener.GetType()}: {ex}");
                    }
                }
            }

            lock (m_cmdLockObject)
            {
                if (m_commands.IsEmpty && m_commandsWithResult.IsEmpty)
                {
                    m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.DispatchCommands);
                }
            }
        }

        //PUBLIC part >>>
        public void SendCommand<T>(T command) where T : ICommand 
        {
            m_commands.Enqueue(new CommandContainer<T>(command));
            lock (m_cmdLockObject)
            {
                m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.DispatchCommands);
            }
        }

        public async Job<CommandResult<TResult>> SendCommandAndReturnResult<T, TResult>(T command, int waitTime = 0) where T : ICommand
        {
           var commandContainer = new CommandContainerWithResult<T, TResult>(command);
            m_commandsWithResult.Enqueue(commandContainer);

            lock (m_cmdLockObject)
            {
                m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.DispatchCommands);
            }

            long endWaitTime = Time.Milliseconds + waitTime;
            await Job.WaitUntil(() => commandContainer.IsDispatched
                                      || commandContainer.IsCanceled
                                      || endWaitTime < Time.Milliseconds);


            if (!commandContainer.IsDispatched && commandContainer.TryCancel())
            {
                return new CommandResult<TResult>(false, default);
            }

            if (commandContainer.IsAsync)
            {
                await commandContainer.WaitForAsyncResult();
            }

            return new CommandResult<TResult>(true, commandContainer.Result);
        }
    }
}
