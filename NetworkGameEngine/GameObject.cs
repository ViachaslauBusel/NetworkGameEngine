using NetworkGameEngine.Signals.Components;

namespace NetworkGameEngine
{
    public sealed partial class GameObject
    {
        private string m_name;
        private bool m_isDestroyed = false;
        private bool m_isActive = false;
        private World m_world;
        private Workflow m_workflow;

        public string Name => m_name;
        public uint ID { get; private set; }
        //public int ThreadID => m_workflow.ThreadID;
        public bool IsDestroyed => m_isDestroyed;
        public World World => m_world;
        internal Workflow Workflow => m_workflow;
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
                m_workflow.CallRegistry.Register(this, MethodType.OnEnableComponent);
            }
            else
            {
                m_workflow.CallRegistry.Register(this, MethodType.OnDisableComponent);
            }
        }

        public bool IsCurrentThreadOwner()
        {
            return m_workflow.ThreadID == Thread.CurrentThread.ManagedThreadId && this == m_workflow.CurrentGameObject;
        }

        public void InjectDependenciesIntoObject(Object component) => m_world.InjectDependenciesIntoObject(component);

        internal void Init(uint objectID, Workflow workflow, World world)
        {
            ID = objectID;
            m_world = world;
            m_workflow = workflow;
            m_isActive = true;
        }

        internal void ScheduleDestroyComponents()
        {
            foreach (var comp in m_components.Values)
            {
                m_outgoingComponents.Add(comp.GetType());
            }
            m_workflow?.CallRegistry.Register(this, MethodType.OnDestroyComponent);
        }

        public void Destroy()
        {
            if (m_world == null)
            {
                throw new InvalidOperationException("GameObject is not part of a world");
            }
            m_world.RemoveGameObject(this.ID);
        }

        internal void FinalizeDestroyObject()
        {
            if (m_components.Count > 0)
            {
                m_world.LogError($"GameObject {Name}:{ID} being destroyed with components still attached.");
            }
            if (!m_isDestroyed)
            {
                m_world.LogError($"GameObject {Name}:{ID} destroyed.");
            }
            m_isDestroyed = true;
            m_isActive = false;
        }
    }
}
