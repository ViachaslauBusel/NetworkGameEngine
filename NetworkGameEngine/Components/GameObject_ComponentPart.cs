using NetworkGameEngine.Components;
using NetworkGameEngine.JobsSystem;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace NetworkGameEngine
{
    public sealed partial class GameObject
    {
        private ConcurrentQueue<Component> m_incomingComponents = new ConcurrentQueue<Component>();
        private ConcurrentBag<Type> m_outgoingComponents = new ConcurrentBag<Type>();
        private Dictionary<Type, Component> m_components = new ();
        private ComponentCallRegistry m_methodComponents = new ();

        internal bool HasIncomingComponents => !m_incomingComponents.IsEmpty;
        internal bool HasUpdateComponents => m_methodComponents.GetTargetsFor(MethodType.UpdateComponent).Any();
        internal bool HasLateUpdateComponents => m_methodComponents.GetTargetsFor(MethodType.LateUpdateComponent).Any();

        public async Job<bool> AddComponentAsync<T>() where T : Component, new()
        {
            return await AddComponentAsync(new T());
        }

        public async Job<bool> AddComponentAsync(Component component)
        {
            AddComponent(component);

            await Job.WaitWhile(() => component.State == ComponentState.None && !IsDestroyed);
            return component.State != ComponentState.Error && component.State != ComponentState.None && !IsDestroyed;
        }

        public void AddComponent<T>() where T : Component, new()
        {
            AddComponent(new T());
        }

        public void AddComponent(Component component)
        {
            lock (m_incomingComponents)
            {
                m_incomingComponents.Enqueue(component);
                m_world?.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.PrepareComponent);
            }
        }

        public T GetComponent<T>() where T : class
        {
            Debug.Assert(ThreadID == Thread.CurrentThread.ManagedThreadId,
                   "Was called by a thread that does not own this data");

            if (m_components.TryGetValue(typeof(T), out Component component))
            {
                return component as T;
            }
            foreach (var c in m_components.Values)
            {
                if (c is T)
                {
                    return c as T;
                }
            }
            return default;
        }

        internal List<T> GetComponents<T>() where T : class
        {
            Debug.Assert(ThreadID == Thread.CurrentThread.ManagedThreadId,
                                  "Was called by a thread that does not own this data");

            List<T> components = new List<T>();
            foreach (var c in m_components.Values)
            {
                if (c is T)
                {
                    components.Add(c as T);
                }
            }

            return components;
        }

        public void DestroyComponent<T>()
        {
            m_outgoingComponents.Add(typeof(T));
            lock (m_outgoingComponents)
            {
                m_world?.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.OnDestroyComponent);
            }
        }

        public void DestroyComponent(Component component)
        {
            m_outgoingComponents.Add(component.GetType());
            lock (m_outgoingComponents)
            {
                m_world?.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.OnDestroyComponent);
            }
        }

        internal void RegisterCallMethodsForComponent(Component component, MethodType methodType)
        {
            m_methodComponents.Register(component, methodType);
            m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, methodType);
        }

        internal void PrepareIncomingComponents()
        {
            while (m_incomingComponents.TryDequeue(out Component newComponent))
            {
                newComponent.InternalInit(this);
                if (m_components.ContainsKey(newComponent.GetType()))
                {
                    newComponent.InternalSetError();
                    continue;
                }
                m_components.Add(newComponent.GetType(), newComponent);
                m_methodComponents.Register(newComponent, MethodType.InitComponent);
            }

            //Register command listener
            var componentsToInit = m_methodComponents.GetTargetsFor(MethodType.InitComponent);
            int count = componentsToInit.Count;
            for (int i = 0; i < count; i++)
            {
                var c = componentsToInit[i];
                RegisterCommandListenersForComponent(c);
                InjectDependenciesIntoObject(c);
            }

            lock (m_incomingComponents)
            {
                if (m_incomingComponents.Count == 0)
                {
                    m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.PrepareComponent);
                }
            }
            m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.InitComponent);
        }


        internal void CallInitComponents()
        {
            var componentsToInit = m_methodComponents.GetTargetsFor(MethodType.InitComponent);
            var span = CollectionsMarshal.AsSpan(componentsToInit);
            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                InitComponentSafe(c);
                if (IsActive && c.Enabled)
                {
                    m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.OnEnableComponent);
                }
            }
            m_methodComponents.Clear(MethodType.InitComponent);
            m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.InitComponent);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void InitComponentSafe(Component component)
        {
            try
            {
                component.Init();
            }
            catch (Exception ex)
            {
                m_world.LogError($"Exception in Init of component {component.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
            }
        }

        /// <summary>
        /// Вызывается при кажждой активации GameObject или компонента если компонент был неактивен
        /// </summary>
        internal void CallOnEnableComponents()
        {
            foreach (var c in m_components.Values)
            {
                try
                {
                    // Если компонент уже включен или не требуется включение, пропускаем вызов
                    bool needEnable = c.Enabled && m_isActive;
                    if (c.IsEnabled || !needEnable) continue;
                    c.OnEnable();
                }
                catch (Exception ex)
                {
                    m_world.LogError($"Exception in OnEnable of component {c.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
                }
                c.SetEnabled();
                //Register update and late update components
                if (c.HasUpdateOverride)
                {
                    RegisterCallMethodsForComponent(c, MethodType.UpdateComponent);
                }
                if (c.HasLateUpdateOverride)
                {
                    RegisterCallMethodsForComponent(c, MethodType.LateUpdateComponent);
                }
                if (!c.IsStarted)
                {
                    RegisterCallMethodsForComponent(c, MethodType.StartComponent);
                }
            }
            //m_methodComponents.Clear(MethodType.OnEnableComponent);
            m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.OnEnableComponent);
        }

        /// <summary>
        /// Вызывается один раз после Init если компонент и GameObject активны или при активации GameObject или компонента
        /// </summary>
        internal void CallOnStartComponents()
        {
            var componentsToStart = m_methodComponents.GetTargetsFor(MethodType.StartComponent);
            var span = CollectionsMarshal.AsSpan(componentsToStart);
            for (int i = 0; i < span.Length; i++)
            {
                var c = span[i];
                if (c.IsStarted) continue;

                StartComponentSafe(c);
                c.SetStarted();
            }
            m_methodComponents.Clear(MethodType.StartComponent);
            m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.StartComponent);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void StartComponentSafe(Component component)
        {
            try
            {
                component.Start();
            }
            catch (Exception ex)
            {
                m_world.LogError($"Exception in Start of component {component.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
            }
        }

        internal void CallOnUpdateComponents()
        {
            if (!m_isActive) return;

            var components = m_methodComponents.GetTargetsFor(MethodType.UpdateComponent);
            var span = CollectionsMarshal.AsSpan(components);
            for (int i = 0; i < span.Length; i++)
            {
                var component = span[i];

                if (!component.Enabled)
                    continue;

                UpdateComponentSafe(component);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void UpdateComponentSafe(Component component)
        {
            try
            {
                component.Update();
            }
            catch (Exception ex)
            {
                m_world.LogError(
                    $"Exception in Update of component {component.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
            }
        }

        internal void CallOnLateUpdateComponents()
        {
            if (!m_isActive) return;
            var components = m_methodComponents.GetTargetsFor(MethodType.LateUpdateComponent);
            var span = CollectionsMarshal.AsSpan(components);
            for (int i = 0; i < span.Length; i++)
            {
                var component = span[i];

                if (!component.Enabled) continue;

                LateUpdateComponentSafe(component);
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private void LateUpdateComponentSafe(Component component)
        {
            try
            {
                component.LateUpdate();
            }
            catch (Exception ex)
            {
                m_world.LogError(
                    $"Exception in LateUpdate of component {component.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
            }
        }


        internal void CallOnDisableComponents()
        {
            foreach (var component in m_components.Values)
            {
                try
                {
                    // Если компонент уже отключен или не требуется отключение, пропускаем вызов
                    bool needDisable = !component.Enabled || !m_isActive;
                    if (!component.IsEnabled || !needDisable) continue;
                        component.OnDisable();
                }
                catch (Exception ex)
                {
                    m_world.LogError($"Exception in OnDisable of component {component.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
                }
                component.SetDisabled();
                if (component.HasUpdateOverride)
                {
                    m_methodComponents.Unregister(component, MethodType.UpdateComponent);
                    if (m_methodComponents.GetTargetsFor(MethodType.UpdateComponent).Count == 0)
                    {
                        m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.UpdateComponent);
                    }
                }
                if (component.HasLateUpdateOverride)
                {
                    m_methodComponents.Unregister(component, MethodType.LateUpdateComponent);
                    if (m_methodComponents.GetTargetsFor(MethodType.LateUpdateComponent).Count == 0)
                    {
                        m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.LateUpdateComponent);
                    }
                }
            }
            //m_methodComponents.Clear(MethodType.OnDisableComponent);
            m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.OnDisableComponent);
        }

        internal void CallOnDestroyComponents()
        {
            while (m_outgoingComponents.TryTake(out Type removeType))
            {
                if (m_components.TryGetValue(removeType, out Component component))
                {
                    component.InternalDestroy();
                    try
                    {
                        component.OnDisable();
                    }
                    catch (Exception ex)
                    {
                        m_world.LogError($"Exception in OnDisable of component {component.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
                    }
                    m_methodComponents.Register(component, MethodType.OnDestroyComponent);
                }
                //Удаляем компоненты которые еще не были добавлены
                lock (m_incomingComponents)
                {
                    if (!m_incomingComponents.IsEmpty)
                    {
                        var kept = new List<Component>(capacity: 8);

                        while (m_incomingComponents.TryDequeue(out var pending))
                        {
                            if (pending.GetType() == removeType)
                            {
                                // Чтобы AddComponentAsync не зависал на State == None
                                pending.InternalSetError();
                                continue;
                            }

                            kept.Add(pending);
                        }

                        foreach (var pending in kept)
                            m_incomingComponents.Enqueue(pending);
                    }
                }
            }

          
            var componentsToDestroy = m_methodComponents.GetTargetsFor(MethodType.OnDestroyComponent);
            int count = componentsToDestroy.Count;
            for (int i = 0; i < count; i++)
            {
                var component = componentsToDestroy[i];
                try
                {
                    component.OnDestroy();
                }
                catch (Exception ex)
                {
                    m_world.LogError($"Exception in OnDestroy of component {component.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
                }

                m_components.Remove(component.GetType());

                UnregisterCommandListenersForComponent(component);

                if (component.HasUpdateOverride)
                {
                    m_methodComponents.Unregister(component, MethodType.UpdateComponent);
                    if (m_methodComponents.GetTargetsFor(MethodType.UpdateComponent).Count() == 0)
                    {
                        m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.UpdateComponent);
                    }
                }
                if (component.HasLateUpdateOverride)
                {
                    m_methodComponents.Unregister(component, MethodType.LateUpdateComponent);
                    if (m_methodComponents.GetTargetsFor(MethodType.LateUpdateComponent).Count() == 0)
                    {
                        m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.LateUpdateComponent);
                    }
                }
            }
            m_methodComponents.Clear(MethodType.OnDestroyComponent);

            lock (m_outgoingComponents)
            {
                if (m_outgoingComponents.Count == 0)
                {
                    m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.OnDestroyComponent);
                }
            }
        }
    }
}
