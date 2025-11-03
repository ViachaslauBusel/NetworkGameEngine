using System.Reflection;
using System.Runtime.Serialization;

namespace NetworkGameEngine
{
    /// <summary>
    /// Данные, унаследованные от этого класса, можно безопасно читать из любого потока,
    /// даже если поток не владеет этим GameObject.
    /// </summary>
    public class PublicModel : LocalModel
    {
        private PublicModel _syncData;

        internal override void Initialize(GameObject gameObject)
        {
           base.Initialize(gameObject);
            _syncData = CreateData();
            SyncData();
        }

        internal override void UpdateData()
        {
            if (IsInternalDirty())
            {
                SyncData();
            }
        }

        // This method is called in the constructor to create a new instance of the derived class.
        private PublicModel CreateData()
        {
            var type = GetType();

            // Allow non-public parameterless constructors to be used
            PublicModel duplicat = (PublicModel)FormatterServices.GetUninitializedObject(type);
            duplicat._isDuplicate = true;
            try
            {
                duplicat.OnDuplicateInitialized();
            }
            catch (Exception ex)
            {
                GameObject.World.LogError($"Exception in OnDuplicateInitialized: {ex}");
            }
            return duplicat;
        }

        // Этот метод копирует все структуры данных из текущего объекта в _syncData.
        // ССылки на объекты не копируются, только значения структур.
        private void SyncData()
        {
            const BindingFlags flags =
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly;

            try
            {
                _syncData.OnBeforeDataSync(this);
            }
            catch (Exception ex)
            {
                GameObject.World.LogError($"Exception in OnBeforeDataSync: {ex}");
            }

            for (var t = GetType(); t != null && t != typeof(PublicModel); t = t.BaseType)
            {
                foreach (var f in t.GetFields(flags))
                {
                    if (f.IsStatic || f.Name == "_isDuplicate") continue;

                    if (f.FieldType.IsValueType)
                    {
                        var value = f.GetValue(this);
                        f.SetValue(_syncData, value);
                    }
                }
            }

            // Локальный снимок синхронизирован
            MarkInternalClean();
            try
            {
                _syncData.OnAfterDataSync(this);
            }
            catch (Exception ex)
            {
                GameObject.World.LogError($"Exception in OnAfterDataSync: {ex}");
            }
        }

        internal override PublicModel GetClone()
        {
            return _syncData;
        }

        public override void MakeAllDirty()
        {
            if (IsDuplicate)
            {
                throw new InvalidOperationException("Attempting to modify SharedDataBlock from a non-owning thread.");
            }
            base.MakeAllDirty();
            ScheduleUpdate();
        }

        /// <summary>
        /// Schedules the object for an update in the next update cycle.
        /// </summary>
        public void ScheduleUpdate()
        {
            base.MarkInternalDirty();
            GameObject?.EnqueueForUpdate(this);
        }


        protected virtual void OnDuplicateInitialized()
        {
        }

        protected virtual void OnBeforeDataSync(LocalModel sourceBlock)
        {
        }

        protected virtual void OnAfterDataSync(LocalModel sourceBlock)
        {
        }
    }
}
