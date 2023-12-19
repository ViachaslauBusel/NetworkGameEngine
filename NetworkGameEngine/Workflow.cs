using System.Diagnostics;

namespace NetworkGameEngine
{
    public enum MethodType { None = 0, Init, Start, Update, LateUpdate, OnDestroy, UpdateData,
        Command,
        Prepare
    }
    public class Workflow
    {
        private Thread m_thread;
        private Object m_locker = new object();
        private Dictionary<int, GameObject> m_objects = new Dictionary<int, GameObject>();
        private volatile MethodType m_currentMethod = MethodType.None;
        private List<MethodType> m_history = new List<MethodType>();

        public bool IsFree => m_currentMethod == MethodType.None;

        public int ThreadID { get; private set; }

        public void Init()
        {
            m_thread = new Thread(ThreadLoop);
            m_thread.Start();
            ThreadID = m_thread.ManagedThreadId;
        }

        internal void AddObject(GameObject obj)
        {
            m_objects.Add(obj.ID, obj);
        }

        internal void CallMethod(MethodType method)
        {
            lock (m_locker)
            {
                if(m_currentMethod != MethodType.None) throw new Exception("invalid object processing state");
                m_currentMethod = method;
                Monitor.PulseAll(m_locker);
            }
        }

        internal void RemoveObject(GameObject removeObj)
        {
            m_objects.Remove(removeObj.ID); 
        }

        internal void Wait()
        {
            lock (m_locker)
            {
                while (m_currentMethod != MethodType.None) { Monitor.Wait(m_locker); }
            }
        }

        private void ThreadLoop()
        {
            lock (m_locker)
            {
                while (true)
                {
                    Monitor.Wait(m_locker);
                    //if(m_objects.Count>0)
                    //{

                    //}
                    m_history.Add(m_currentMethod);
                    switch (m_currentMethod)
                    {
                        case MethodType.Prepare:
                            foreach (GameObject obj in m_objects.Values) { obj.CallPrepare(); }
                            break;
                        case MethodType.Init:
                            foreach (GameObject obj in m_objects.Values) { obj.CallInit(); }
                            break;
                        case MethodType.Start:
                            foreach (GameObject obj in m_objects.Values) { obj.CallStart(); }
                            break;
                        case MethodType.Update:
                            foreach (GameObject obj in m_objects.Values) { obj.CallUpdate(); }
                            break;
                        case MethodType.Command:
                            foreach (GameObject obj in m_objects.Values) { obj.CallCommand(); }
                            break;
                        case MethodType.LateUpdate:
                            foreach (GameObject obj in m_objects.Values) { obj.CallLateUpdate(); }
                            break;
                        case MethodType.OnDestroy:
                            foreach (GameObject obj in m_objects.Values) { obj.CallOnDestroy(); }
                            break;
                        case MethodType.UpdateData:
                            foreach (GameObject obj in m_objects.Values) { obj.CallUpdateData(); }
                            break;
                        default:
                            throw new Exception("invalid object processing state");
                    }
                    m_currentMethod = MethodType.None;
                    Monitor.PulseAll(m_locker);
                }
            }
        }
    }
}
