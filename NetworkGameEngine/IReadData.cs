using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine
{
    public interface IReadData<T> where T : struct
    {
        void UpdateData(ref T data);
    }
}
