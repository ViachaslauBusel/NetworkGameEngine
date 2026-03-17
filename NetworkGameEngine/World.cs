using Autofac;
using NetworkGameEngine.DependencyInjection;
using NetworkGameEngine.Diagnostics;
using NetworkGameEngine.Interfaces;
using NetworkGameEngine.Tools;
using NetworkGameEngine.Workflows;
using System.Collections.Concurrent;
using System.Reflection;

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
        private List<AddingObjectTask> m_addedObjectTasks = new List<AddingObjectTask>();
        private ConcurrentQueue<RemovingObjectTask> m_removeObjects = new ConcurrentQueue<RemovingObjectTask>();
        private List<GameObject> m_removedObjects = new List<GameObject>();
        private List<IMainThreadUpdatableService> _updatableServices = new List<IMainThreadUpdatableService>();
        private List<IMultiThreadUpdatableService> _threadAwareUpdatableServices = new List<IMultiThreadUpdatableService>();
        private Dictionary<Type, List<MethodInfo>> m_injectMethodsCache = new Dictionary<Type, List<MethodInfo>>();
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

        private T[] ResolveAll<T>()
        {
            var result = new List<T>();

            foreach (var container in m_containers)
            {
                // Для IEnumerable<T> Autofac безопасно возвращает пустую коллекцию,
                // если регистраций нет.
                result.AddRange(container.Resolve<IEnumerable<T>>());
            }

            return result.ToArray();
        }

        public void InjectDependenciesIntoObject(Object component)
        {
            lock (m_injectMethodsCache)
            {
                var type = component.GetType();

                // Cache method lookup to avoid redundant reflection
                if (!m_injectMethodsCache.TryGetValue(type, out var methods))
                {
                    methods = ReflectionHelper.GetAllMethods(type)
                                              .Where(m => m.GetCustomAttributes(typeof(InjectAttribute), true).Any())
                                              .ToList();

                    m_injectMethodsCache[type] = methods;
                }

                foreach (var method in methods)
                {
                    // Cache resolved dependencies for the method parameters
                    var parameters = method.GetParameters()
                                           .Select(p => Resolve(p.ParameterType))
                                           .ToArray();

                    method.Invoke(component, parameters);
                }
            }
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
            builder.RegisterType<MainThreadDelayDispatcher>().AsSelf().As<IMainThreadUpdatableService>()
                                                                      .As<IMultiThreadUpdatableService>().SingleInstance();
            builder.RegisterType<TimedTaskScheduler>().FindConstructorsWith(type => type.GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                                                      .AsSelf().InstancePerDependency();
            m_containers.Add(builder.Build());

            m_workflows.Init(maxThread);


            _updatableServices.AddRange(ResolveAll<IMainThreadUpdatableService>());
            _threadAwareUpdatableServices.AddRange(ResolveAll<IMultiThreadUpdatableService>());
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
                        else LogError($"Frame is taking too long! Time from tick: {m_time.CalculateTimeFromTick()} ms");
                    }
                } 
                catch(Exception ex)
                {
                    LogError($"Fatal error in world thread: {ex}");
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
            int addObjectsCount = Math.Min(400, m_addObjects.Count);
            for (int i = 0; i < addObjectsCount && m_addObjects.TryDequeue(out var task); i++)
            {
                GameObject obj = task.GameObject;
                Workflow workflow = m_workflows.GetWorkflowByIndex(m_addObjectThIndex);
                obj.Init(m_generatorID++, workflow, this);

                m_objects.TryAdd(obj.ID, obj);

                workflow.AddObject(obj);
                m_addObjectThIndex = (m_addObjectThIndex + 1) % m_workflows.Count;

                task.GameObjectID = obj.ID;
                m_addedObjectTasks.Add(task);
            }

            ExecuteMethod(MethodType.PrepareComponent);
            ExecuteMethod(MethodType.PrepareModel);
            ExecuteMethod(MethodType.InitComponent);
            ExecuteMethod(MethodType.OnAttachModel);
            ExecuteMethod(MethodType.OnEnableComponent);
            ExecuteMethod(MethodType.StartComponent);
            ExecuteMethod(MethodType.UpdateComponent);
            ExecuteMethod(MethodType.DispatchCommands);
            ExecuteMethod(MethodType.JobExecutor);
            ExecuteMethod(MethodType.LateUpdateComponent);

            int removeObjectsCount = m_removeObjects.Count;
            for (int i = 0; i < removeObjectsCount && m_removeObjects.TryDequeue(out var task); i++)
            {
                bool isObjectFound = m_objects.ContainsKey(task.GameObjectID);
                if (isObjectFound)
                {
                    GameObject removeObj = m_objects[task.GameObjectID];
                    m_removedObjects.Add(removeObj);

                    removeObj.ScheduleDestroyComponent();
                }
                task.Completed(isObjectFound);
            }

            ExecuteMethod(MethodType.OnDetachModel);
            ExecuteMethod(MethodType.OnDisableComponent);
            ExecuteMethod(MethodType.OnDestroyComponent);
            ExecuteMethod(MethodType.UpdateModels);

            foreach (var obj in m_removedObjects)
            {
                m_objects.TryRemove(obj.ID, out _);
                obj.Workflow.RemoveObject(obj);
                obj.FinalizeDestroyObject();
            }
            m_removedObjects.Clear();

            _executuinProfiler?.StartMethodProfiling(MethodType.OneThreadService);
            foreach (var service in _updatableServices)
            {
                try
                {
                    service.Update();
                }
                catch (Exception ex)
                {
                    LogError($"Error in service {service.GetType().Name}: {ex}");
                }
            }
            _executuinProfiler?.StopMethodProfiling(MethodType.OneThreadService);

            _executuinProfiler?.StartMethodProfiling(MethodType.MultiThreadService);

            foreach (var service in _threadAwareUpdatableServices)
            {
                m_workflows.RunMultiThreadService(service);
            }

            if (m_addedObjectTasks.Count > 0)
            {
                foreach (var addedTask in m_addedObjectTasks)
                {
                    addedTask.Completed(true);
                }
                m_addedObjectTasks.Clear();
            }

            _executuinProfiler?.StopMethodProfiling(MethodType.MultiThreadService);
        }

        private void ExecuteMethod(MethodType method)
        {
            _executuinProfiler?.StartMethodProfiling(method);
            m_workflows.CallMethod(method);
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
