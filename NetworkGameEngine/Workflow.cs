using NetworkGameEngine.JobsSystem;

namespace NetworkGameEngine
{
    public enum MethodType { None = 0, Init, Start, Update, LateUpdate, OnDestroy, UpdateData,
        Command,
        Prepare,
        JobEcxecutor,
        ActionExecuter
    }
    public class Workflow
    {
        private World m_world;
        private Thread m_thread;
        private Object m_locker = new object();
        private List<GameObject> m_objects = new List<GameObject>();
        private Action _action;
        private volatile MethodType m_currentMethod = MethodType.None;
        private ThreadJobExecutor m_jobExcecutor;

        public bool IsFree => m_currentMethod == MethodType.None;

        public int ThreadID { get; private set; }

        public void Init(World world)
        {
            m_world = world;
            m_thread = new Thread(ThreadLoop);
            m_thread.Start();
            ThreadID = m_thread.ManagedThreadId;
        }

        private void InitThread()
        {
            m_jobExcecutor = JobsManager.RegisterThreadHandler();
        }

        internal void AddObject(GameObject obj)
        {
            m_objects.Add(obj);
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

        internal void Execute(Action action)
        {
            lock (m_locker)
            {
                if (m_currentMethod != MethodType.None) throw new Exception("invalid object processing state");
                _action = action;
                m_currentMethod = MethodType.ActionExecuter;
                Monitor.PulseAll(m_locker);
            }
        }

        internal void RemoveObject(GameObject removeObj)
        {
            m_objects.Remove(removeObj);
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
            InitThread();
            lock (m_locker)
            {
                while (true)
                {
                    Monitor.Wait(m_locker);

                    int executedObjectIndex = 0;
                    while (m_currentMethod != MethodType.None)
                    {
                        try
                        {
                            switch (m_currentMethod)
                            {
                                case MethodType.Prepare:
                                    for(; executedObjectIndex < m_objects.Count; executedObjectIndex++) 
                                    { m_objects[executedObjectIndex].CallPrepare(); }
                                    break;
                                case MethodType.Init:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    { m_objects[executedObjectIndex].CallInit(); }
                                    break;
                                case MethodType.Start:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++) 
                                    { m_objects[executedObjectIndex].CallStart(); }
                                    break;
                                case MethodType.Update:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++) 
                                    { m_objects[executedObjectIndex].CallUpdate(); }
                                    break;
                                case MethodType.Command:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++) 
                                    { m_objects[executedObjectIndex].CallCommand(); }
                                    break;
                                case MethodType.JobEcxecutor:
                                    m_jobExcecutor.Update();
                                    break;
                                case MethodType.LateUpdate:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++) 
                                    { m_objects[executedObjectIndex].CallLateUpdate(); }
                                    break;
                                case MethodType.OnDestroy:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    { m_objects[executedObjectIndex].CallOnDestroy(); }
                                    break;
                                case MethodType.UpdateData:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    { m_objects[executedObjectIndex].CallUpdateData(); }
                                    break;
                                case MethodType.ActionExecuter:
                                    _action?.Invoke();
                                    break;
                                default:
                                    throw new Exception("invalid object processing state");
                            }
                        }
                        catch (Exception e)
                        {
                            m_world.LogError($"Error in {m_objects[executedObjectIndex].Name}.{m_currentMethod} method: {e.Message}");
                            executedObjectIndex++;
                            continue;
                        }
                        m_currentMethod = MethodType.None;
                    }
                    Monitor.PulseAll(m_locker);
                }
            }
        }

      
    }
}
