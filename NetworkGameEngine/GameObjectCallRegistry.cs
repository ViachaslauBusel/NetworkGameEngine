namespace NetworkGameEngine
{
    internal sealed class GameObjectCallRegistry
    {
        private Dictionary<MethodType, HashSet<GameObject>> m_methodObjects = new Dictionary<MethodType, HashSet<GameObject>>();
        private List<GameObject> m_cachedList = new List<GameObject>();
        private object m_lockObject = new object();

        internal IEnumerable<GameObject> GetTargetsFor(MethodType methodType)
        {
            lock (m_lockObject)
            {
                m_cachedList.Clear();
                if (!m_methodObjects.ContainsKey(methodType))
                {
                    return m_cachedList;
                }
               
                m_cachedList.AddRange(m_methodObjects[methodType]);
                return m_cachedList;
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

                m_methodObjects[methodType].Add(gameObject);
            }
        }

        internal void Unregister(GameObject gameObject, MethodType methodType)
        {
            lock (m_lockObject)
            {
                if (m_methodObjects.ContainsKey(methodType))
                {
                    m_methodObjects[methodType].Remove(gameObject);
                }
            }
        }
    }
}
