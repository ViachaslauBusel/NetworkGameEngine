using Autofac;
using NetworkGameEngine.DependencyInjection;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace NetworkGameEngine
{
    public sealed partial class GameObject
    {
        private string m_name;
        private int m_threadID = 0;
        private bool m_isDestroyed = false;
        private bool _isActive = true;
        private World m_world;
        private ConcurrentBag<Component> m_incomigComponents = new ConcurrentBag<Component>();
        private ConcurrentBag<Type> m_outgoingComponents = new ConcurrentBag<Type>();
        private LinkedList<Component> m_components = new LinkedList<Component>();
        private LinkedList<Component> m_updateComponents = new LinkedList<Component>();
        private LinkedList<Component> m_lateUpdateComponents = new LinkedList<Component>();
        private List<Component> m_newComponents = new List<Component>();
        private List<Component> m_removeComponents = new List<Component>();
        Dictionary<Type, List<MethodInfo>> m_injectMethodsCache = new Dictionary<Type, List<MethodInfo>>();

        public string Name => m_name;
        public uint ID { get; private set; }
        public int ThreadID => m_threadID;
        public bool IsDestroyed => m_isDestroyed;
        public World World => m_world;
        public bool IsActive => _isActive;

        public GameObject(string name)
        {
            m_name = name;
        }
        public GameObject()
        {
            m_name = "GameObject";
        }

        public void SetActive(bool value)
        {
            if (m_threadID != Thread.CurrentThread.ManagedThreadId)
            {
                throw new InvalidOperationException("Attempting to change active state from a thread that does not own the object");
            }
            _isActive = value;
        }

        public bool IsCurrentThreadOwner()
        {
            return m_threadID == Thread.CurrentThread.ManagedThreadId;
        }

        public void AddComponent(Component component)//ref 
        {
           //if(m_threadID != 0 && m_threadID != Thread.CurrentThread.ManagedThreadId)
           //{
           //    // Debug.Log.Fatal($"Attempting to add a component to a thread that does not own the object");
           //}
           
            m_incomigComponents.Add(component);
        }

        public void AddComponent<T>() where T : Component, new()
        {
            AddComponent(new T());  
        }

        public void DestroyComponent<T>() 
        {
            m_outgoingComponents.Add(typeof(T));
        }

        public void DestroyComponent(Component component)
        {
            m_outgoingComponents.Add(component.GetType());
        }

        /// <summary>
        /// Register new components
        /// </summary>
        internal void CallPrepare()
        {
            PrepareIncomingBlockData();
            while (m_incomigComponents.TryTake(out Component newComponent))
            {
                if (m_components.Any(c => newComponent.GetType() == c.GetType()))
                {
                    //  Debug.Log.Error($"It is impossible to re-add a component, such a component already exists on the object");
                    continue;
                }
                newComponent.InternalInit(this);
                m_components.AddLast(newComponent);
                m_newComponents.Add(newComponent);
            }
            //Register command listener
            foreach (var c in m_newComponents)
            {
                foreach (var @interface in c.GetType().GetInterfaces().Where(x => x.IsGenericType))
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(IReactCommand<>)
                        || @interface.GetGenericTypeDefinition() == typeof(IReactCommandWithResult<,>))
                    {
                        AddListener(@interface.GenericTypeArguments[0], c);
                    }
                }

                //Register update and late update components
                if (c.HasUpdateOverride)
                {
                    m_updateComponents.AddLast(c);
                }
                if (c.HasLateUpdateOverride)
                {
                    m_lateUpdateComponents.AddLast(c);
                }
            }
            //Inject dependencies
            foreach (var c in m_newComponents)
            {
                InjectDependenciesIntoObject(c);
            }
        }

        internal void InjectDependenciesIntoObject(Object component)
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

        internal void CallInit()
        {
            foreach (var c in m_newComponents)
            {
                 c.Init();
            }
            CallOnEnableForAllData();
        }

        internal void CallStart()
        {
            foreach (var c in m_newComponents) 
            {
                c.Start();
            }
            m_newComponents.Clear();
        }

        internal void CallUpdate()
        {
            if (!_isActive) return;
            foreach (var c in m_updateComponents) { if(c.enabled) c.Update(); }
        }

        internal void CallLateUpdate()
        {
            if (!_isActive) return;
            foreach (var c in m_lateUpdateComponents) { if(c.enabled) c.LateUpdate(); }
        }
        internal void Init(uint objectID, int threadID, World world)
        {
            ID = objectID;
            m_threadID = threadID;
            m_world = world;
        }

        internal void Destroy()
        {
            m_removeComponents.AddRange(m_components);
            m_components.Clear();
            m_updateComponents.Clear();
            m_lateUpdateComponents.Clear();
            m_isDestroyed = true;
        }

        internal void CallOnDestroy()
        {
            while (m_outgoingComponents.TryTake(out Type removeType))
            {
                var component = m_components.FirstOrDefault(c => c.GetType() == removeType);
                if (component != null)
                {
                    m_removeComponents.Add(component);
                    m_components.Remove(component);
                    m_updateComponents.Remove(component);
                    m_lateUpdateComponents.Remove(component);
                }
            }
            foreach (var c in m_removeComponents) { c.OnDestroy(); }
            //Unregister command listener
            foreach (var c in m_removeComponents)
            {
                foreach (var @interface in c.GetType().GetInterfaces().Where(x => x.IsGenericType))
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(IReactCommand<>)
                        || @interface.GetGenericTypeDefinition() == typeof(IReactCommandWithResult<,>))
                    {
                        RemoveListener(@interface.GenericTypeArguments[0], c);
                    }
                }
            }
            m_removeComponents.Clear();
            PrepareOutgoingBlockData();
        }

        public T GetComponent<T>() where T : class
        {
            Debug.Assert(ThreadID == Thread.CurrentThread.ManagedThreadId,
                   "Was called by a thread that does not own this data");

            var value = m_components.FirstOrDefault(c => c is T);
            if (value != null)
            {
                return value as T;
            }
            return default;
        }

        internal List<T> GetComponents<T>() where T : class
        {
            Debug.Assert(ThreadID == Thread.CurrentThread.ManagedThreadId,
                                  "Was called by a thread that does not own this data");

            List<T> components = new List<T>();
            foreach (var c in m_components)
            {
                if (c is T)
                {
                    components.Add(c as T);
                }
            }

            return components;
        }
    }
}
