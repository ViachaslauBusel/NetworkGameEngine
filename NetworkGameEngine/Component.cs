using NetworkGameEngine.JobsSystem;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine
{
    public abstract class Component
    {
        private GameObject m_gameObject;

        public GameObject GameObject => m_gameObject;

        public bool enabled = true;

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
            Debug.Assert(m_gameObject.ThreadID == Thread.CurrentThread.ManagedThreadId,
                               "Was called by a thread that does not own this data");
            return m_gameObject.GetComponent<T>();
        }

        public List<T> GetComponents<T>() where T : class
        {
            Debug.Assert(m_gameObject.ThreadID == Thread.CurrentThread.ManagedThreadId,
                                              "Was called by a thread that does not own this data");
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
    }
}
