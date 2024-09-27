using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.Interfaces
{
    public interface IUpdatableService
    {
        int Priority { get; }
        void Update();
    }
}
