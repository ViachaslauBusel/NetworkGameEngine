using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine
{
    internal class DataContainer<T> : DataContainer where T : struct
    {
        private T m_object;
        private dynamic m_updater;

        internal override object Object => m_object;
        internal override void Init(object reactComponent) 
        {
            m_object = default(T);
            m_updater = reactComponent;
        }

        internal override void UpdateData()
        {
            m_updater.UpdateData(ref m_object);
        }
    }
    internal abstract class DataContainer
    {
        internal abstract object Object { get; }
        internal abstract void Init(object reactComponent);
        internal abstract void UpdateData();

       // internal abstract void Read<T>(ref T data) where T : struct;

    }
    public sealed partial class GameObject
    {
        private Dictionary<Type, DataContainer> m_registeredData = new();
        private Object m_dataLocker = new Object();

        internal void AddData(Type dataType, object reactComponent)
        {
            lock (m_dataLocker)
            {
                if (m_registeredData.ContainsKey(dataType))
                {
                    throw new Exception($"[{dataType}]This type is already registered");
                }
                var type = typeof(DataContainer<>).MakeGenericType(dataType);
                DataContainer container = (DataContainer)Activator.CreateInstance(type);
                container.Init(reactComponent);
                m_registeredData.Add(dataType, container);
            }
        }

        internal void RemoveData(Type dataType) 
        {
            lock (m_dataLocker)
            {
                if (m_registeredData.ContainsKey(dataType))
                {
                    m_registeredData.Remove(dataType);
                }
            }
        }

        public void ReadData<T>(out T data) where T : struct 
        {
            lock(m_dataLocker) 
            {
                Type dataType = typeof(T);
               
                if (m_registeredData.ContainsKey(dataType))
                {
                    data = (T)m_registeredData[dataType].Object;
                    return;
                }
                data = default(T);
            }
        }
        public List<T> ReadAllData<T>() where T : class
        { 
            List<T> list = new List<T>();   
            lock (m_dataLocker) 
            {
                foreach(var data in m_registeredData.Values)
                {
                    if(data.Object is T matchdata)
                    {
                        list.Add(matchdata); 
                    } 
                }
            }
            return list;
        }

        internal void CallUpdateData()
        {
            lock (m_dataLocker)
            {
                foreach(DataContainer data in m_registeredData.Values)
                {
                    data.UpdateData();
                }
            }
        }
    }
}
