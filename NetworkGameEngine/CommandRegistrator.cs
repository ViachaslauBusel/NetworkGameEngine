using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine
{
    internal static class CommandRegistrator
    {
        //IReactCommand<Command>, Command
        private static Dictionary<Type, Type> m_commands = new Dictionary<Type, Type>();
        static CommandRegistrator()
        {
            foreach (var @type in Assembly.GetExecutingAssembly().GetTypes())
            {
                foreach (var @interface in @type.GetInterfaces())
                {
                    if (@interface.IsGenericType && @interface.GetGenericTypeDefinition() == typeof(IReactCommand<>))
                    {
                        var genericArg = @interface.GetGenericArguments();
                        if (!m_commands.ContainsKey(@interface.GetType()) && genericArg.Length > 0)
                        {
                           m_commands.Add(@interface.GetType(), genericArg[0]);
                        }
                    }
                }
            }
        }
    }
}
