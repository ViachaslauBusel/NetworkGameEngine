namespace NetworkGameEngine.Components
{
    internal class ComponentCallRegistry
    {
        private Dictionary<MethodType, LinkedList<Component>> m_components = new Dictionary<MethodType, LinkedList<Component>>();

        internal IEnumerable<Component> GetTargetsFor(MethodType methodType)
        {
            if (!m_components.ContainsKey(methodType))
            {
                m_components[methodType] = new LinkedList<Component>();
            }
            return m_components[methodType];
        }

        internal void Register(Component newComponent, MethodType init)
        {
            if (!m_components.ContainsKey(init))
            {
                m_components[init] = new LinkedList<Component>();
            }
            if (m_components[init].Contains(newComponent)) return;
            m_components[init].AddLast(newComponent);
        }

        internal void Unregister(Component component, MethodType update)
        {
            if (m_components.ContainsKey(update))
            {
                m_components[update].Remove(component);
            }
        }

        internal void Clear(MethodType init)
        {
            if (m_components.ContainsKey(init))
            {
                m_components[init].Clear();
            }
        }
    }
}
