using NetworkGameEngine.JobsSystem;

namespace NetworkGameEngine
{
    public enum MethodType { 
        None = 0,
        PrepareComponent,
        PrepareModel,
        InitComponent, 
        OnAttachModel,
        OnEnableComponent, 
        StartComponent,
        UpdateComponent,
        LateUpdateComponent,
        OnDetachModel,
        OnDisableComponent, 
        OnDestroyComponent,
        UpdateModels,
        DispatchCommands,
        JobExecutor,
        ActionExecuter,
        OneThreadService,
        MultiThreadService
    }
    internal class Workflow
    {
        private World m_world;
        private Thread m_thread;
        private int m_threadId;
        private Object m_locker = new object();
        private List<GameObject> m_objects = new List<GameObject>();
        private GameObjectCallRegistry _callRegistry = new GameObjectCallRegistry();
        private Action _action;
        private volatile MethodType m_currentMethod = MethodType.None;
        private ThreadJobExecutor m_jobExcecutor;
        // Текущий GameObject, обрабатываемый в этом потоке
        private GameObject m_currentObject;

        public GameObject CurrentGameObject => m_currentObject;
        public bool IsFree => m_currentMethod == MethodType.None;
        public int ThreadID => m_threadId;
        internal GameObjectCallRegistry CallRegistry => _callRegistry;

        public void Init(World world)
        {
            m_world = world;
            m_thread = new Thread(ThreadLoop);
            m_thread.Start();
            m_threadId = m_thread.ManagedThreadId;
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
            if (obj.HasIncomingComponents)
                _callRegistry.Register(obj, MethodType.PrepareComponent);
            if (obj.HasIncomingModels)
                _callRegistry.Register(obj, MethodType.PrepareModel);
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

                    while (m_currentMethod != MethodType.None)
                    {
                        try
                        {
                            switch (m_currentMethod)
                            {
                                case MethodType.PrepareComponent:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.PrepareComponent))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.PrepareIncomingComponents();
                                    }
                                    break;
                                    case MethodType.PrepareModel:
                                        foreach (var obj in _callRegistry.GetTargetsFor(MethodType.PrepareModel))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.PrepareIncomingModels();
                                    }
                                        break;
                                case MethodType.InitComponent:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.InitComponent))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.CallInitComponents();
                                    }
                                    break;
                                    case MethodType.OnAttachModel:
                                        foreach (var obj in _callRegistry.GetTargetsFor(MethodType.OnAttachModel))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.CallOnAttachModels();  
                                    }
                                        break;
                                case MethodType.OnEnableComponent:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.OnEnableComponent))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.CallOnEnableComponents();
                                    }
                                    break;
                                case MethodType.StartComponent:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.StartComponent))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.CallOnStartComponents();
                                    }
                                    break;
                                case MethodType.UpdateComponent:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.UpdateComponent))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.CallOnUpdateComponents();
                                    }
                                    break;
                                case MethodType.DispatchCommands:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.DispatchCommands))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.DispatchPendingCommands();
                                    }
                                    break;
                                case MethodType.JobExecutor:
                                    m_jobExcecutor.Update();
                                    break;
                                case MethodType.LateUpdateComponent:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.LateUpdateComponent))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.CallOnLateUpdateComponents();
                                    }
                                    break;
            
                                case MethodType.OnDisableComponent:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.OnDisableComponent))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.CallOnDisableComponents();
                                    }
                                    break;
                                case MethodType.OnDetachModel:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.OnDetachModel))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.CallOnDetachModels(); 
                                    }
                                    break;
                                case MethodType.OnDestroyComponent:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.OnDestroyComponent))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.CallOnDestroyComponents();
                                    }
                                    break;
                                case MethodType.UpdateModels:
                                    foreach (var obj in _callRegistry.GetTargetsFor(MethodType.UpdateModels))
                                    {
                                        m_currentObject = obj;
                                        m_currentObject.CallUpdateModels();
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
                        }
                        catch (Exception e)
                        {
                            m_world.LogError($"Fatal Error in method {m_currentMethod} of Workflow: {e.Message}");
                            continue;
                        }
                        m_currentMethod = MethodType.None;
                        m_currentObject = null;
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
