using NetworkGameEngine.Models;
using NetworkGameEngine.Signals.Commands;
using NetworkGameEngine.Workflows;
using System.Runtime.CompilerServices;

namespace NetworkGameEngine
{
    public abstract class PriorityEventBase<THandler> : IlSyncedReference where THandler : Delegate
    {
        public readonly struct SubscriptionEntry
        {
            public readonly THandler Handler;
            public readonly GameObject Owner;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public SubscriptionEntry(THandler handler, GameObject owner)
            {
                Handler = handler;
                Owner = owner;
            }
        }

        private readonly object m_lock = new object();
        private readonly GameObject m_ownerGameObject;
        private readonly SortedList<int, List<SubscriptionEntry>> m_subscriptionsByPriority =
            new SortedList<int, List<SubscriptionEntry>>(Comparer<int>.Default);
        private List<SubscriptionEntry> m_snapshot = new List<SubscriptionEntry>(15);
        private bool m_isDirty = false;


        protected GameObject OwnerObject => m_ownerGameObject;

        protected PriorityEventBase()
        {
            m_ownerGameObject = GlobalWorkflowRegistry.GetCurrentWorkflow()?.CurrentGameObject;
        }

        // ----------------------------------------------------------------------
        // Subscribe / Unsubscribe
        // ----------------------------------------------------------------------
        public void Subscribe(THandler handler, int order = 0)
        {
            var currentObj = GlobalWorkflowRegistry.GetCurrentWorkflow()?.CurrentGameObject;

            lock (m_lock)
            {
                if (!m_subscriptionsByPriority.TryGetValue(order, out var list))
                {
                    list = new List<SubscriptionEntry>(10);
                    m_subscriptionsByPriority.Add(order, list);
                }

                list.Add(new SubscriptionEntry(handler, currentObj));
                m_isDirty = true;
            }
        }

        public void Unsubscribe(THandler handler)
        {
            lock (m_lock)
            {
                foreach (var list in m_subscriptionsByPriority.Values)
                {
                    for (int i = list.Count - 1; i >= 0; i--)
                    {
                        if (list[i].Handler.Equals(handler))
                            list.RemoveAt(i);
                    }
                }
                m_isDirty = true;
            }
        }

        // ----------------------------------------------------------------------
        // Snapshot enumeration
        // ----------------------------------------------------------------------

        protected List<SubscriptionEntry> CreateSnapshot()
        {
            lock (m_lock)
            {
                if (!m_isDirty)
                    return m_snapshot;
                int total = 0;
                foreach (var kv in m_subscriptionsByPriority)
                    total += kv.Value.Count;

                m_snapshot.Clear();

                foreach (var kv in m_subscriptionsByPriority)
                    m_snapshot.AddRange(kv.Value);

                return m_snapshot;
            }
        }

        // ----------------------------------------------------------------------
        // Invocation routing
        // ----------------------------------------------------------------------

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void InternalInvoke(in SubscriptionEntry entry, Action call)
        {
            if (entry.Owner != null && entry.Owner != m_ownerGameObject)
            {
                entry.Owner.SendCommand(new ExecuteActionCommand(call));
            }
            else
            {
                call?.Invoke();
            }
        }
    }

    // ==========================================================================
    // EVENTS
    // ==========================================================================

    public sealed class PriorityEvent : PriorityEventBase<Action>
    {
        public void Invoke()
        {
            var snapshot = CreateSnapshot();
            foreach (var entry in snapshot)
                InternalInvoke(entry, entry.Handler);
        }
    }

    public sealed class PriorityEvent<T0> : PriorityEventBase<Action<T0>>
    {
        public void Invoke(T0 a0)
        {
            var snapshot = CreateSnapshot();
            foreach (var entry in snapshot)
                InternalInvoke(entry, () => entry.Handler(a0));
        }
    }

    public sealed class PriorityEvent<T0, T1> : PriorityEventBase<Action<T0, T1>>
    {
        public void Invoke(T0 a0, T1 a1)
        {
            var snapshot = CreateSnapshot();
            foreach (var entry in snapshot)
                InternalInvoke(entry, () => entry.Handler(a0, a1));
        }
    }

    public sealed class PriorityEvent<T0, T1, T2> : PriorityEventBase<Action<T0, T1, T2>>
    {
        public void Invoke(T0 a0, T1 a1, T2 a2)
        {
            var snapshot = CreateSnapshot();
            foreach (var entry in snapshot)
                InternalInvoke(entry, () => entry.Handler(a0, a1, a2));
        }
    }
}
