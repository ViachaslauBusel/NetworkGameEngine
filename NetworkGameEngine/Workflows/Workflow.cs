using NetworkGameEngine.JobsSystem;

namespace NetworkGameEngine
{
    public enum MethodType { None = 0, Init, Start, Update, LateUpdate, OnDestroy, UpdateData,
        Command,
        Prepare,
        JobExecutor,
        ActionExecuter,
        OneThreadService,
        MultiThreadService
    }
    internal class Workflow
    {
        private World m_world;
        private Thread m_thread;
        private Object m_locker = new object();
        private List<GameObject> m_objects = new List<GameObject>();
        private Action _action;
        private volatile MethodType m_currentMethod = MethodType.None;
        private ThreadJobExecutor m_jobExcecutor;
        // Текущий GameObject, обрабатываемый в этом потоке
        private GameObject m_currentObject;

        public GameObject CurrentGameObject => m_currentObject;
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
            // Устанавливаем контекст синхронизации на поток мира ДО любых async-операций
            System.Threading.SynchronizationContext.SetSynchronizationContext(
                new NetworkGameEngine.JobsSystem.EngineSynchronizationContext(m_world));

            m_jobExcecutor = JobsManager.RegisterThreadHandler(m_world, this);
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
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    {
                                        m_currentObject = m_objects[executedObjectIndex];
                                        m_currentObject.CallPrepare();
                                    }
                                    break;
                                case MethodType.Init:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    {
                                        m_currentObject = m_objects[executedObjectIndex];
                                        m_currentObject.CallInit();
                                    }
                                    break;
                                case MethodType.Start:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    {
                                        m_currentObject = m_objects[executedObjectIndex];
                                        m_currentObject.CallStart();
                                    }
                                    break;
                                case MethodType.Update:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    {
                                        m_currentObject = m_objects[executedObjectIndex];
                                        m_currentObject.CallUpdate();
                                    }
                                    break;
                                case MethodType.Command:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    {
                                        m_currentObject = m_objects[executedObjectIndex];
                                        m_currentObject.DispatchPendingCommands();
                                    }
                                    break;
                                case MethodType.JobExecutor:
                                    m_jobExcecutor.Update();
                                    break;
                                case MethodType.LateUpdate:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    {
                                        m_currentObject = m_objects[executedObjectIndex];
                                        m_currentObject.CallLateUpdate();
                                    }
                                    break;
                                case MethodType.OnDestroy:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    {
                                        m_currentObject = m_objects[executedObjectIndex];
                                        m_currentObject.CallOnDestroy();
                                    }
                                    break;
                                case MethodType.UpdateData:
                                    for (; executedObjectIndex < m_objects.Count; executedObjectIndex++)
                                    {
                                        m_currentObject = m_objects[executedObjectIndex];
                                        m_currentObject.CallUpdateData();
                                    }
                                    break;
                                case MethodType.ActionExecuter:
                                    try
                                    {
                                        _action?.Invoke();
                                    }
                                    catch (Exception e)
                                    {
                                        m_world.LogError($"Error in Workflow ActionExecuter: {e.Message}");
                                    }
                                    break;
                                default:
                                    throw new Exception("invalid object processing state");
                            }
                            m_currentObject = null;
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

        internal void SetCurrentGameObject(GameObject owner)
        {
            m_currentObject = owner;
        }
    }
}
