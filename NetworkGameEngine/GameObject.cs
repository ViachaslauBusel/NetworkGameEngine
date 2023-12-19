using System.Collections.Concurrent;
using System.ComponentModel;
using System.Security.Cryptography;

namespace NetworkGameEngine
{
    public sealed partial class GameObject
    {
        private int m_threadID = 0;
        private World m_world;
        private ConcurrentBag<Component> m_incomigComponents = new ConcurrentBag<Component>();
        private ConcurrentBag<Type> m_outgoingComponents = new ConcurrentBag<Type>();
        private LinkedList<Component> m_components = new LinkedList<Component>();
        private List<Component> m_newComponents = new List<Component>();
        private List<Component> m_removeComponents = new List<Component>();
        private ConcurrentBag<Task> m_tasks = new ConcurrentBag<Task>();

  
        public int ID { get; private set; }
        public int ThreadID => m_threadID;
        public bool IsInitialized => m_tasks.All(t => t.IsCompleted);

      
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

        public void RemoveComponent<T>() 
        {
            m_outgoingComponents.Add(typeof(T));
            //if (m_threadID != 0 && m_threadID != Thread.CurrentThread.ManagedThreadId)
            //{
            //  //  Debug.Log.Fatal($"Attempting to add a component to a thread that does not own the object");
            //}
        }

        internal void CallPrepare()
        {
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
            //Register data and command listener
            foreach (var c in m_newComponents)
            {
                foreach (var @interface in c.GetType().GetInterfaces().Where(x => x.IsGenericType))
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(IReactCommand<>))
                    {
                        AddListener(@interface.GenericTypeArguments[0], c);
                    }
                    else if (@interface.GetGenericTypeDefinition() == typeof(IReadData<>))
                    {
                        AddData(@interface.GenericTypeArguments[0], c);
                    }
                }
            }
            foreach (var c in m_newComponents) { m_world.DiContainer.Inject(c); }
        }

        internal void CallInit()
        {
          
            foreach (var c in m_newComponents) { m_tasks.Add(c.Init()); }
        }

        internal void CallStart()
        {
            foreach (var c in m_newComponents) { c.Start(); }
            m_newComponents.Clear();
        }

        internal void CallUpdate()
        {
            foreach (var c in m_components) { c.Update(); }
        }

        internal void CallLateUpdate()
        {
            foreach (var c in m_components) { c.LateUpdate(); }
        }
        internal void Init(int objectID, int threadID, World world)
        {
            ID = objectID;
            m_threadID = threadID;
            m_world = world;
        }

        internal void Destroy()
        {
            m_removeComponents.AddRange(m_components);
            m_components.Clear();
        }

        internal void CallOnDestroy()
        {
            while(m_outgoingComponents.TryTake(out Type removeType))
            {
                var component = m_components.First(c => c.GetType() == removeType);
                if (component != null)
                {
                    m_removeComponents.Add(component);
                    m_components.Remove(component);
                }
            }
            foreach (var c in m_removeComponents) { c.OnDestroy(); }
            //Unregister command listener
            foreach (var c in m_newComponents)
            {
                foreach (var @interface in c.GetType().GetInterfaces().Where(x => x.IsGenericType))
                {
                    if (@interface.GetGenericTypeDefinition() == typeof(IReactCommand<>))
                    {
                        RemoveListener(@interface.GenericTypeArguments[0], c);
                    }
                    else if (@interface.GetGenericTypeDefinition() == typeof(IReadData<>))
                    {
                        RemoveData(@interface.GenericTypeArguments[0]);
                    }
                }
            }
            m_removeComponents.Clear();
        }

        internal T GetComponent<T>() where T : Component
        {
          return m_components.FirstOrDefault(c => c is T) as T;
        }

       
    }
}
