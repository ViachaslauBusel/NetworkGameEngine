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
        private bool m_isActive = false;
        private World m_world;

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

        public void InjectDependenciesIntoObject(Object component) => m_world.InjectDependenciesIntoObject(component);

        internal void Init(uint objectID, int threadID, World world)
        {
            ID = objectID;
            m_threadID = threadID;
            m_world = world;
            m_isActive = true;
        }

        internal void ScheduleDestroy()
        {
            foreach (var comp in m_components)
            {
                m_outgoingComponents.Add(comp.GetType());
            }
            m_world?.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.OnDestroyComponent);
            m_isDestroyed = true;
        }

        public void Destroy()
        {
            if (m_world == null)
            {
                throw new InvalidOperationException("GameObject is not part of a world");
            }
            m_world.RemoveGameObject(this.ID);
        }
    }
}
