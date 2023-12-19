using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine
{
    public abstract class Component
    {
        private GameObject m_gameObject;

        protected GameObject GameObject => m_gameObject;

        internal void InternalInit(GameObject obj)
        {
            m_gameObject = obj;
        }
        public virtual async Task Init() { }
        public virtual void Start() { }
        public virtual void Update() { }
        public virtual void LateUpdate() { }
        public virtual void OnDestroy() { }

        protected T GetComponent<T>() where T : Component => m_gameObject.GetComponent<T>();

    }
}
