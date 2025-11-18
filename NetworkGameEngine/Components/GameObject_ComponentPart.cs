using NetworkGameEngine.Components;
using NetworkGameEngine.JobsSystem;
using System.Collections.Concurrent;
using System.ComponentModel;
using System.Diagnostics;

namespace NetworkGameEngine
{
    public sealed partial class GameObject
    {
        private ConcurrentQueue<Component> m_incomingComponents = new ConcurrentQueue<Component>();
        private ConcurrentBag<Type> m_outgoingComponents = new ConcurrentBag<Type>();
        private LinkedList<Component> m_components = new LinkedList<Component>();
        private ComponentCallRegistry m_methodComponents = new();

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

            var value = m_components.FirstOrDefault(c => c is T);
            if (value != null)
            {
                return value as T;
            }
            return default;
        }

        internal List<T> GetComponents<T>() where T : class
        {
            Debug.Assert(ThreadID == Thread.CurrentThread.ManagedThreadId,
                                  "Was called by a thread that does not own this data");

            List<T> components = new List<T>();
            foreach (var c in m_components)
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
                if (m_components.Any(c => newComponent.GetType() == c.GetType()))
                {
                    newComponent.InternalSetError();
                    continue;
                }
                newComponent.InternalInit(this);
                m_components.AddLast(newComponent);
                m_methodComponents.Register(newComponent, MethodType.InitComponent);
            }

            //Register command listener
            foreach (var c in m_methodComponents.GetTargetsFor(MethodType.InitComponent))
            {
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
            foreach (var c in m_methodComponents.GetTargetsFor(MethodType.InitComponent))
            {
                try
                {
                    c.Init();
                }
                catch (Exception ex)
                {
                    m_world.LogError($"Exception in Init of component {c.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
                }
                if (IsActive && c.Enabled)
                {
                    m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Register(this, MethodType.OnEnableComponent);
                }
            }
            m_methodComponents.Clear(MethodType.InitComponent);
            m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.InitComponent);
        }

        /// <summary>
        /// Вызывается при кажждой активации GameObject или компонента если компонент был неактивен
        /// </summary>
        internal void CallOnEnableComponents()
        {
            foreach (var c in m_components)
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
            foreach (var c in m_methodComponents.GetTargetsFor(MethodType.StartComponent))
            {
                try
                {
                    if (!c.IsStarted)
                    c.Start();
                }
                catch (Exception ex)
                {
                    m_world.LogError($"Exception in Start of component {c.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
                }
                c.SetStarted();
            }
            m_methodComponents.Clear(MethodType.StartComponent);
            m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.StartComponent);
        }

        internal void CallOnUpdateComponents()
        {
            if (!m_isActive) return;
            foreach (var c in m_methodComponents.GetTargetsFor(MethodType.UpdateComponent))
            {
                if (c.Enabled)
                {
                    try
                    {
                        c.Update();
                    }
                    catch (Exception ex)
                    {
                        m_world.LogError($"Exception in Update of component {c.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
                    }
                }
            }
        }

        internal void CallOnLateUpdateComponents()
        {
            if (!m_isActive) return;
            foreach (var c in m_methodComponents.GetTargetsFor(MethodType.LateUpdateComponent))
            {
                if (c.Enabled)
                {
                    try
                    {
                        c.LateUpdate();
                    }
                    catch (Exception ex)
                    {
                        m_world.LogError($"Exception in LateUpdate of component {c.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
                    }
                }
            }
        }


        internal void CallOnDisableComponents()
        {
            foreach (var component in m_components)
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
            //m_methodComponents.Clear(MethodType.OnDisableComponent);
            m_world.Workflows.GetWorkflowByThreadId(ThreadID).CallRegistry.Unregister(this, MethodType.OnDisableComponent);
        }

        internal void CallOnDestroyComponents()
        {
            while (m_outgoingComponents.TryTake(out Type removeType))
            {
                var component = m_components.FirstOrDefault(c => c.GetType() == removeType);
                if (component != null)
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
            }

            foreach (var component in m_methodComponents.GetTargetsFor(MethodType.OnDestroyComponent))
            {
                try
                {
                    component.OnDestroy();
                }
                catch (Exception ex)
                {
                    m_world.LogError($"Exception in OnDestroy of component {component.GetType().Name} on GameObject {Name} (ID: {ID}): {ex}");
                }

                m_components.Remove(component);

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
