using NetworkGameEngine.Sync;
using System.Reflection;
using System.Runtime.Serialization;

namespace NetworkGameEngine
{
    public abstract class DataBlock : SyncMarkers
    {
        private GameObject _gameObject;
        private DataBlock _syncData;

        public GameObject GameObject => _gameObject;
        public bool IsDuplicate => _gameObject == null;

        /// <summary>
        /// Метод которыйвызываеться при добовлении этого DataBlock к GameObject
        /// </summary>
        public virtual void OnAttached() { }

        /// <summary>
        /// Метод который вызываеться при удалении этого DataBlock из GameObject 
        /// или при удалении самого GameObject
        /// </summary>
        public virtual void OnDetached() { }

        internal DataBlock GetClone()
        {
            return _syncData;
        }

        internal void UpdateData()
        {
            if (IsDirtyAndMarkClean(SyncMarkerType.Local))
            {
                SyncData();
            }
        }

        internal void Initialize(GameObject gameObject)
        {
            _gameObject = gameObject;
            _syncData = CreateData();
            SyncData();
        }

        // This method is called in the constructor to create a new instance of the derived class.
        private DataBlock CreateData()
        {
            var type = GetType();

            // Allow non-public parameterless constructors to be used
            return (DataBlock)FormatterServices.GetUninitializedObject(type);
        }

        // Этот метод копирует все структуры данных из текущего объекта в _syncData.
        // ССылки на объекты не копируются, только значения структур.
        private void SyncData()
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            for (var t = GetType(); t != null && t != typeof(DataBlock); t = t.BaseType)
            {
                foreach (var f in t.GetFields(flags))
                {
                    if (f.IsStatic) continue;

                    if (f.FieldType.IsValueType)
                    {
                        var value = f.GetValue(this);
                        f.SetValue(_syncData, value);
                    }
                }
            }

            // Локальный снимок синхронизирован
            MarkAsClean(SyncMarkerType.Local);
        }

        ///------------------------------OVERRIDE------------------------------///
        public override void MakeAllDirty()
        {
            base.MakeAllDirty();
            _gameObject?.EnqueueForUpdate(this);
        }
    }
}
