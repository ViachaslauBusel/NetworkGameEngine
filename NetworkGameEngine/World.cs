using Autofac;
using NetworkGameEngine.Diagnostics;
using NetworkGameEngine.Interfaces;
using NetworkGameEngine.Tools;
using NetworkGameEngine.Workflows;
using System.Collections.Concurrent;

namespace NetworkGameEngine
{
    public class AddingObjectTask : TaskBase
    {
       public GameObject GameObject { get; set; }
        public uint GameObjectID { get; set; } = 0;
    }
    public class RemovingObjectTask : TaskBase
    {
        public uint GameObjectID { get; set; }
    }
    public class World
    {
        private List<IContainer> m_containers;
        private ConcurrentDictionary<uint, GameObject> m_objects = new ConcurrentDictionary<uint, GameObject>();
        private ConcurrentQueue<AddingObjectTask> m_addObjects = new ConcurrentQueue<AddingObjectTask>();
        private ConcurrentQueue<RemovingObjectTask> m_removeObjects = new ConcurrentQueue<RemovingObjectTask>();
        private List<GameObject> m_removedObjects = new List<GameObject>();
        private List<IUpdatableService> _updatableServices = new List<IUpdatableService>();
        private List<IThreadAwareUpdatableService> _threadAwareUpdatableServices = new List<IThreadAwareUpdatableService>();
        private uint m_generatorID = 1;
        private WorkflowPool m_workflows;
        private int m_addObjectThIndex = 0;
        private bool m_isWorking = false;
        private Time m_time;
        private MethodExecutionProfiler _executuinProfiler;

        public event Action<string> OnLog;

        public Time Time => m_time;
        public int ActiveGameObjectCount => m_objects.Count;
        public int PendingGameObjectAddCount => m_addObjects.Count;
        internal WorkflowPool Workflows => m_workflows;

        public MethodExecutionProfiler StartExecutionProfiler(int maxSamples = 1000)
        {
            _executuinProfiler = new MethodExecutionProfiler(maxSamples);
            return _executuinProfiler;
        }

        public void StopExecutionProfiler()
        {
            _executuinProfiler = null;
        }

        public object Resolve(Type type)
        {
           foreach (var container in m_containers)
            {
                if (container.IsRegistered(type))
                {
                    return container.Resolve(type);
                }
            }
            return null;
        }

        public T Resolve<T>()
        {
            foreach (var container in m_containers)
            {
                if (container.IsRegistered(typeof(T)))
                {
                    return container.Resolve<T>();
                }
            }
            return default;
        }

        /// <summary>
        /// Initialize the world
        /// </summary>
        /// <param name="maxThread">Count of threads that will be used to process objects</param>
        /// <param name="frameInterval">Time between frames</param>
        /// <param name="container">Container for dependency injection</param>
        public void Init(int maxThread, int frameInterval, IContainer container = null)
        {
            m_time = new Time(frameInterval);
            m_workflows = new WorkflowPool(this);
            m_containers = new List<IContainer>();

            if(container != null)
                m_containers.Add(container);

            var builder = new ContainerBuilder();
            builder.RegisterInstance(m_time).AsSelf().SingleInstance();
            builder.RegisterInstance(this).AsSelf().SingleInstance();
            m_containers.Add(builder.Build());

            m_workflows.Init(maxThread);


            _updatableServices.AddRange(Resolve<IUpdatableService[]>());
            _threadAwareUpdatableServices.AddRange(Resolve<IThreadAwareUpdatableService[]>());
            m_isWorking = true;
            Thread th = new Thread(WorldThread);
            th.IsBackground = true;
            th.Start();
        }

        public void Stop()
        {
            m_isWorking = false;
        }

        private void WorldThread()
        {
            // Устанавливаем контекст синхронизации на поток мира ДО любых async-операций
            System.Threading.SynchronizationContext.SetSynchronizationContext(
                new NetworkGameEngine.JobsSystem.EngineSynchronizationContext(this));

            while (m_isWorking)
            {
                try
                {
                    while (true)
                    {
                        m_time.NextTick();
                        Update();
                        Console.Title = $"NetworkGameEngine | Tick: {m_time.CalculateTimeFromTick()} ms | Objects: {m_objects.Count}";
                        int sleepTime = m_time.CalculateSleepTime();
                        if(sleepTime > 0) Thread.Sleep(sleepTime);
                    }
                } 
                catch(Exception ex)
                {
                    LogError(ex.Message);
                }
            }
        }

        public async Task<uint> AddGameObject(GameObject obj)
        {
            var task = new AddingObjectTask() { GameObject = obj };
            m_addObjects.Enqueue(task);
            await task.Wait();

            return task.GameObjectID;
        }

        public void RemoveGameObject(uint gameObjectID) 
        {
            m_removeObjects.Enqueue(new RemovingObjectTask() { GameObjectID = gameObjectID });
        }

        internal void Update()
        {
            int addObjectsCount = Math.Min(100, m_addObjects.Count);
            for (int i = 0; i < addObjectsCount && m_addObjects.TryDequeue(out var task); i++)
            {
                GameObject obj = task.GameObject;
                obj.Init(m_generatorID++, m_workflows.GetWorkflowByIndex(m_addObjectThIndex).ThreadID, this);

                m_objects.TryAdd(obj.ID, obj);

                m_workflows.GetWorkflowByIndex(m_addObjectThIndex).AddObject(obj);
                m_addObjectThIndex = (m_addObjectThIndex + 1) % m_workflows.Count;

                task.GameObjectID = obj.ID;
                task.Completed(true);
            }

            ExecuteMethod(MethodType.Prepare);
            ExecuteMethod(MethodType.Init);
            ExecuteMethod(MethodType.Start);
            ExecuteMethod(MethodType.Update);
            ExecuteMethod(MethodType.Command);
            ExecuteMethod(MethodType.JobExecutor);
            ExecuteMethod(MethodType.LateUpdate);

            int removeObjectsCount = m_removeObjects.Count;
            for (int i = 0; i < removeObjectsCount && m_removeObjects.TryDequeue(out var task); i++)
            {
                bool isObjectFound = m_objects.ContainsKey(task.GameObjectID);
                if (isObjectFound)
                {
                    GameObject removeObj = m_objects[task.GameObjectID];
                    m_removedObjects.Add(removeObj);

                    removeObj.Destroy();
                }
                task.Completed(isObjectFound);
            }

            ExecuteMethod(MethodType.OnDestroy);
            ExecuteMethod(MethodType.UpdateData);

            foreach (var obj in m_removedObjects)
            {
                m_objects.TryRemove(obj.ID, out _);
                m_workflows.GetWorkflowByThreadID(obj.ThreadID).RemoveObject(obj);
            }
            m_removedObjects.Clear();

            _executuinProfiler?.StartMethodProfiling(MethodType.OneThreadService);
            foreach (var service in _updatableServices)
            {
                service.Update();
            }
            _executuinProfiler?.StopMethodProfiling(MethodType.OneThreadService);

            _executuinProfiler?.StartMethodProfiling(MethodType.MultiThreadService);
            foreach (var service in _threadAwareUpdatableServices)
            {
                for (int i = 0; i < m_workflows.Count; i++) { m_workflows.GetWorkflowByIndex(i).Execute(() => service.Update(i, m_workflows.Count)); }
                foreach (var th in m_workflows.AllWorkflows) { th.Wait(); }
            }
            _executuinProfiler?.StopMethodProfiling(MethodType.MultiThreadService);
        }

        private void ExecuteMethod(MethodType method)
        {
            _executuinProfiler?.StartMethodProfiling(method);
            foreach (var th in m_workflows.AllWorkflows) { th.CallMethod(method); }
            foreach (var th in m_workflows.AllWorkflows) { th.Wait(); }
            _executuinProfiler?.StopMethodProfiling(method);
        }

        public bool TryGetGameObject(uint objectID, out GameObject obj) => m_objects.TryGetValue(objectID, out obj);

        public GameObject FindGameObject(Predicate<GameObject> match)
        {
            foreach (var obj in m_objects.Values)
            {
                if (match(obj))
                    return obj;
            }
            return null;
        }

        internal void LogError(string msg)
        {
            OnLog?.Invoke(msg);
        }
    }
}
