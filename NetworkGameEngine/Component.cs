using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;

namespace NetworkGameEngine
{
    public abstract class Component
    {
        private GameObject m_gameObject;

        public GameObject GameObject => m_gameObject;

        public bool enabled = true;

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

        public T InjectDependenciesIntoObject<T>(T t)
        {
            GameObject.InjectDependenciesIntoObject(t);
            return t;
        }

        public T GetData<T>(int key = 0) where T : DataBlock => m_gameObject.GetData<T>(key);
        public List<T> GetAllData<T>() where T : DataBlock => m_gameObject.GetAllData<T>();
        public bool TryGetData<T>(int key, out T result) where T : DataBlock => m_gameObject.TryGetData<T>(key, out result);
    }
}
