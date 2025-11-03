using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace NetworkGameEngine
{
    internal enum ComponentState
    {
        None = 0,
        Initialized,
        Destroyed,
        Error
    }
    public abstract class Component
    {
        // Cached override detection >>>
        private readonly struct OverrideInfo
        {
            public readonly bool Update;
            public readonly bool LateUpdate;
            public OverrideInfo(bool update, bool lateUpdate)
            {
                Update = update;
                LateUpdate = lateUpdate;
            }
        }

        private GameObject m_gameObject;
        private volatile ComponentState m_state = ComponentState.None;

        public GameObject GameObject => m_gameObject;
        internal ComponentState State => m_state;
        public bool enabled = true;



        private static readonly ConcurrentDictionary<Type, OverrideInfo> s_overrideCache = new();

        internal bool HasUpdateOverride => GetOverrideInfo(GetType()).Update;
        internal bool HasLateUpdateOverride => GetOverrideInfo(GetType()).LateUpdate;

        private static OverrideInfo GetOverrideInfo(Type type)
        {
            return s_overrideCache.GetOrAdd(type, t =>
            {
                return new OverrideInfo(
                    IsMethodOverridden(t, nameof(Update)),
                    IsMethodOverridden(t, nameof(LateUpdate))
                );
            });
        }

        private static bool IsMethodOverridden(Type type, string methodName)
        {
            const BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;
            var method = type.GetMethod(methodName, flags, binder: null, types: Type.EmptyTypes, modifiers: null);
            if (method == null) return false;
            var baseDef = method.GetBaseDefinition();
            return method.DeclaringType != baseDef.DeclaringType;
        }
        // Cached override detection <<<

        internal void InternalInit(GameObject obj)
        {
            m_gameObject = obj;
            m_state = ComponentState.Initialized;
        }
        internal void InternalDestroy()
        {
            m_state = ComponentState.Destroyed;
        }
        internal void InternalSetError()
        {
            m_state = ComponentState.Error;
        }
        public virtual void Init() { }
        public virtual void Start() { }
        public virtual void Update() { }
        public virtual void LateUpdate() { }
        public virtual void OnDestroy() { }

        public T GetComponent<T>() where T : class
        {
            return m_gameObject.GetComponent<T>();
        }

        public List<T> GetComponents<T>() where T : class
        {
            return m_gameObject.GetComponents<T>();
        }

        public void DestroyComponent<T>() where T : Component
        {
            Debug.Assert(m_gameObject.ThreadID == Thread.CurrentThread.ManagedThreadId,
                                                             "Was called by a thread that does not own this data");
            m_gameObject.DestroyComponent<T>();
        }

        public void DestroyComponent(Component component)
        {
            Debug.Assert(m_gameObject.ThreadID == Thread.CurrentThread.ManagedThreadId,
                                                             "Was called by a thread that does not own this data");
            m_gameObject.DestroyComponent(component);
        }

        public T InjectDependenciesIntoObject<T>(T t)
        {
            GameObject.InjectDependenciesIntoObject(t);
            return t;
        }

        public T GetModel<T>(int key = 0) where T : LocalModel => m_gameObject.GetModel<T>(key);
        public List<T> GetAllModel<T>() where T : LocalModel => m_gameObject.GetAllModel<T>();
        public bool TryGetModel<T>(int key, out T result) where T : LocalModel => m_gameObject.TryGetModel<T>(key, out result);
    }
}
