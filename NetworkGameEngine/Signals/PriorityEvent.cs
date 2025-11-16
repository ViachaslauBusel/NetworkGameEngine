namespace NetworkGameEngine.Signals
{
    namespace NetworkGameEngine.Signals
    {
        public abstract class PriorityEventBase<THandler> where THandler : Delegate
        {
            private readonly SortedList<int, List<THandler>> _handlers
                = new SortedList<int, List<THandler>>(new AscComparer());

            public void Subscribe(THandler handler, int order = 0)
            {
                if (!_handlers.TryGetValue(order, out var list))
                {
                    list = new List<THandler>();
                    _handlers.Add(order, list);
                }
                list.Add(handler);
            }

            public void Unsubscribe(THandler handler)
            {
                foreach (var list in _handlers.Values)
                    list.Remove(handler);
            }

            protected IEnumerable<THandler> EnumerateHandlers()
            {
                foreach (var kvp in _handlers)
                {
                    foreach (var handler in kvp.Value)
                        yield return handler;
                }
            }

            private class AscComparer : IComparer<int>
            {
                public int Compare(int x, int y) => x.CompareTo(y);
            }
        }

        public class PriorityEvent : PriorityEventBase<Action>
        {
            public void Invoke()
            {
                foreach (var handler in EnumerateHandlers())
                    handler();
            }
        }

        public class PriorityEvent<T0> : PriorityEventBase<Action<T0>> where T0 : allows ref struct
        {
            public void Invoke(T0 arg0)
            {
                foreach (var handler in EnumerateHandlers())
                    handler(arg0);
            }
        }

        public class PriorityEvent<T0, T1> : PriorityEventBase<Action<T0, T1>> where T0 : allows ref struct
        {
            public void Invoke(T0 arg0, T1 arg1)
            {
                foreach (var handler in EnumerateHandlers())
                    handler(arg0, arg1);
            }
        }
    }
}
