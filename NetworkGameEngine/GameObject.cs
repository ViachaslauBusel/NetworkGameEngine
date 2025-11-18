using Autofac;
using NetworkGameEngine.DependencyInjection;
using NetworkGameEngine.Signals.Components;
using System.Reflection;

namespace NetworkGameEngine
{
    public sealed partial class GameObject
    {
        private string m_name;
        private int m_threadID = 0;
        private bool m_isDestroyed = false;
        private bool m_isActive = true;
        private World m_world;
        private Dictionary<Type, List<MethodInfo>> m_injectMethodsCache = new Dictionary<Type, List<MethodInfo>>();

        public string Name => m_name;
        public uint ID { get; private set; }
        public int ThreadID => m_threadID;
        public bool IsDestroyed => m_isDestroyed;
        public World World => m_world;
        public bool IsActive => m_isActive;

        public GameObject(string name)
        {
            m_name = name;
            AddComponent<EventHandlerComponent>();

        }
        public GameObject(): this("GameObject")
        {
        }

        public void SetActive(bool value)
        {
            //if (m_threadID != Thread.CurrentThread.ManagedThreadId)
            //{
            //    throw new InvalidOperationException("Attempting to change active state from a thread that does not own the object");
            //}
            m_isActive = value;
            if (m_isActive)
            {
                m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.OnEnableComponent);
            }
            else
            {
                m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.OnDisableComponent);
            }
        }

        public bool IsCurrentThreadOwner()
        {
            return m_threadID == Thread.CurrentThread.ManagedThreadId && this == m_world.Workflows.GetCurrentWorkflow().CurrentGameObject;
        }

        public void InjectDependenciesIntoObject(Object component)
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
                                       .Select(p => m_world.Resolve(p.ParameterType))
                                       .ToArray();

                method.Invoke(component, parameters);
            }
        }


        internal void Init(uint objectID, int threadID, World world)
        {
            ID = objectID;
            m_threadID = threadID;
            m_world = world;
        }

        internal void Destroy()
        {
            foreach (var comp in m_components)
            {
                m_outgoingComponents.Add(comp.GetType());
            }
            m_world?.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.OnDestroyComponent);
            m_isDestroyed = true;
        }
    }
}
