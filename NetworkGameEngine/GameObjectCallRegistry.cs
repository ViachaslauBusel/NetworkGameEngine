namespace NetworkGameEngine
{
    internal class GameObjectCallRegistry
    {
        private Dictionary<MethodType, HashSet<GameObject>> m_methodObjects = new Dictionary<MethodType, HashSet<GameObject>>();
        private object m_lockObject = new object();

        internal IEnumerable<GameObject> GetTargetsFor(MethodType methodType)
        {
            lock (m_lockObject)
            {
                if (!m_methodObjects.ContainsKey(methodType))
                {
                    m_methodObjects[methodType] = new HashSet<GameObject>();
                }
                return m_methodObjects[methodType].ToHashSet();
            }
        }

        internal void Register(GameObject gameObject, MethodType methodType)
        {
            lock (m_lockObject)
            {
                if (!m_methodObjects.ContainsKey(methodType))
                {
                    m_methodObjects[methodType] = new HashSet<GameObject>();
                }

                if (m_methodObjects[methodType].Contains(gameObject)) return;

                m_methodObjects[methodType].Add(gameObject);
            }
        }

        internal void Unregister(GameObject gameObject, MethodType update)
        {
            lock (m_lockObject)
            {
                if (m_methodObjects.ContainsKey(update))
                {
                    m_methodObjects[update].Remove(gameObject);
                }
            }
        }
    }
}
