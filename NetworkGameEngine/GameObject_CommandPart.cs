using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using static NetworkGameEngine.GameObject;

namespace NetworkGameEngine
{
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
            reactCommand.ReactCommand(m_command);
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
            IntPtr ptr = Marshal.AllocHGlobal(Marshal.SizeOf<T>());
            Marshal.StructureToPtr(command, ptr, false);
            m_commands.Enqueue(new CommandContainer<T>(command));
        }
    }
}
