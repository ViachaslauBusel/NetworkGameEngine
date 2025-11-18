using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine
{
    public interface IReactCommandWithResult<T, TResult> where T : ICommand
    {
        TResult ReactCommand(ref T command);
    }
}
