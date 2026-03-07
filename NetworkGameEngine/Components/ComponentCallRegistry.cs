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
                _components[i] = new List<Component>();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal List<Component> GetTargetsFor(MethodType type)
        {
            return _components[(int)type];
        }

        internal void Register(Component component, MethodType type)
        {
            var list = _components[(int)type];
            var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(list);

            for (int i = 0; i < span.Length; i++)
            {
                if (ReferenceEquals(span[i], component))
                    return;
            }

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
