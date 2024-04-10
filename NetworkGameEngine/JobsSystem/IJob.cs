using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.JobsSystem
{
 
    public interface IJob
    {
        bool IsCompleted { get; }

        bool TryFinalize();

        void Wait();
    }
}
