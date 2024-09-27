using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.Interfaces
{
    public interface IThreadAwareUpdatableService
    {
        int Priority { get; }
        void Update(int threadIndex, int totalThreads);
    }
}
