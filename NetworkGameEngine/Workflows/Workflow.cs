using NetworkGameEngine.JobsSystem;
using NetworkGameEngine.Workflows;
using System.Runtime.InteropServices;

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
        OneThreadService,
        MultiThreadService
    }
    internal sealed class Workflow
    {
        private readonly Dictionary<MethodType, Action> m_directHandlersByMethod;
        private readonly Dictionary<MethodType, Action<GameObject>> m_gameObjectHandlersByMethod;
        private readonly List<GameObject> m_registeredObjects = new List<GameObject>();
        private readonly GameObjectCallRegistry m_callRegistry = new GameObjectCallRegistry();
        private readonly WorkflowPool m_workflowPool;
        private readonly ManualResetEventSlim m_startEvent;
        private readonly SemaphoreSlim m_dispatchSignal;
        private readonly CountdownEvent m_barrier;

        private World m_world;
        private Thread m_workerThread;
        private int m_workerThreadId;
        private int m_workerThreadIndex;
        private ThreadJobExecutor m_jobExecutor;
        // Текущий GameObject, обрабатываемый в этом потоке
        private GameObject m_currentGameObject;
        private int m_lastProcessedDispatchVersion;

        public GameObject CurrentGameObject => m_currentGameObject;
        public int ThreadID => m_workerThreadId;
        internal GameObjectCallRegistry CallRegistry => m_callRegistry;

        public Workflow(WorkflowPool workflowPool, SemaphoreSlim dispatchSignal, CountdownEvent barrier, int threadIndex)
        {
            m_workflowPool = workflowPool;
            m_dispatchSignal = dispatchSignal;
            m_barrier = barrier;
            m_workerThreadIndex = threadIndex;
            m_directHandlersByMethod = new Dictionary<MethodType, Action>
            {
                [MethodType.JobExecutor] = () => m_jobExecutor.Update(),
                [MethodType.MultiThreadService] = () => m_workflowPool.CurrentMultiThreadService?.Update(m_workerThreadIndex, m_workflowPool.Count)
            };

            m_gameObjectHandlersByMethod = new()
            {
                { MethodType.PrepareComponent, o => o.PrepareIncomingComponents() },
                { MethodType.PrepareModel, o => o.PrepareIncomingModels() },
                { MethodType.InitComponent, o => o.CallInitComponents() },
                { MethodType.OnAttachModel, o => o.CallOnAttachModels() },
                { MethodType.OnEnableComponent, o => o.CallOnEnableComponents() },
                { MethodType.StartComponent, o => o.CallOnStartComponents() },
                { MethodType.UpdateComponent, o => o.CallOnUpdateComponents() },
                { MethodType.LateUpdateComponent, o => o.CallOnLateUpdateComponents() },
                { MethodType.DispatchCommands, o => o.DispatchPendingCommands() },
                { MethodType.UpdateModels, o => o.CallUpdateModels() },
                { MethodType.OnDisableComponent, o => o.CallOnDisableComponents() },
                { MethodType.OnDetachModel, o => o.CallOnDetachModels() },
                { MethodType.OnDestroyComponent, o => o.CallOnDestroyComponents() }
            };
        }

        public void Init(World world)
        {
            m_world = world;
            m_workerThread = new Thread(ThreadLoop);
            m_workerThread.Priority = ThreadPriority.AboveNormal;
            m_workerThread.Start();
            m_workerThreadId = m_workerThread.ManagedThreadId;
        }

        private void InitThread()
        {
            // Устанавливаем контекст синхронизации на поток мира ДО любых async-операций
            System.Threading.SynchronizationContext.SetSynchronizationContext(
                new NetworkGameEngine.JobsSystem.EngineSynchronizationContext(m_world));

            m_jobExecutor = JobsManager.RegisterThreadHandler(m_world, this);
        }

        internal void AddObject(GameObject obj)
        {
            m_registeredObjects.Add(obj);
            if (obj.HasIncomingComponents)
                m_callRegistry.Register(obj, MethodType.PrepareComponent);
            if (obj.HasIncomingModels)
                m_callRegistry.Register(obj, MethodType.PrepareModel);
        }

        internal void RemoveObject(GameObject removeObj)
        {
            m_registeredObjects.Remove(removeObj);
        }

        private void ThreadLoop()
        {
            InitThread();

            while (true)
            {
                m_dispatchSignal.Wait();

                try
                {
                    ExecuteMethod(m_workflowPool.ScheduledMethod);
                }
                catch (Exception exception)
                {
                    m_world.LogError($"Fatal Error in method {m_workflowPool.ScheduledMethod} of Workflow: {exception}");
                }
                finally
                {
                    m_barrier.Signal();
                }
            }
        }

        private void ExecuteMethod(MethodType method)
        {
            if (m_gameObjectHandlersByMethod.TryGetValue(method, out var handler))
            {
                var targets = m_callRegistry.GetTargetsFor(method);
                var span = CollectionsMarshal.AsSpan(targets);

                for (int i = 0; i < span.Length; i++)
                {
                    m_currentGameObject = span[i];
                    handler(m_currentGameObject);
                }
            }
            else if (m_directHandlersByMethod.TryGetValue(method, out var directHandler))
            {
                directHandler();
            }
        }

        internal void SetCurrentGameObject(GameObject owner)
        {
            m_currentGameObject = owner;
        }
    }
}
