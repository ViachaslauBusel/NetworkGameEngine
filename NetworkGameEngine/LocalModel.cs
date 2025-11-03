using NetworkGameEngine.Sync;

namespace NetworkGameEngine
{
    public abstract class LocalModel : SyncMarkers
    {
        private GameObject _gameObject;
        protected internal bool _isDuplicate = false;

        public GameObject GameObject => _gameObject;
        public bool IsDuplicate => _isDuplicate;

        /// <summary>
        /// Метод которыйвызываеться при добовлении этого DataBlock к GameObject
        /// </summary>
        public virtual void OnAttached() { }

        /// <summary>
        /// Метод который вызываеться при удалении этого DataBlock из GameObject 
        /// или при удалении самого GameObject
        /// </summary>
        public virtual void OnDetached() { }

        internal virtual LocalModel GetClone()
        {
            return null;
        }

        internal virtual void UpdateData()
        {
        }

        internal virtual void Initialize(GameObject gameObject)
        {
            _gameObject = gameObject;
        }
    }
}
