using System.Runtime.CompilerServices;

namespace NetworkGameEngine.Components
{
    internal sealed class ComponentCallRegistry
    {
        private readonly List<Component>[] _components;

        public ComponentCallRegistry()
        {
            var count = Enum.GetValues<MethodType>().Length;
            _components = new List<Component>[count];

            for (int i = 0; i < count; i++)
                _components[i] = new List<Component>(40);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal List<Component> GetTargetsFor(MethodType type)
        {
            return _components[(int)type];
        }

        internal void Register(Component component, MethodType type)
        {
            var list = _components[(int)type];

            if (!list.Contains(component))
                list.Add(component);
        }

        internal void Unregister(Component component, MethodType type)
        {
            _components[(int)type].Remove(component);
        }

        internal void Clear(MethodType type)
        {
            _components[(int)type].Clear();
        }
    }
}
