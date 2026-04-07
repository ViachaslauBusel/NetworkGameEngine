using NetworkGameEngine.Workflows;
using System.Collections.Concurrent;

namespace NetworkGameEngine
{
    internal readonly struct DataRemoveRequest
    {
        public readonly Type Type;
        public readonly int? Key;
        public readonly bool AllOfType;
        public DataRemoveRequest(Type type, int? key, bool allOfType)
        {
            Type = type; Key = key; AllOfType = allOfType;
        }
    }

    internal readonly struct DataAddRequest
    {
        public readonly Type Type;
        public readonly int Key;
        public readonly LocalModel Data;
        public DataAddRequest(Type type, int key, LocalModel data)
        {
            Type = type; Key = key; Data = data;
        }
    }

    public partial class GameObject
    {
        // Основное хранилище данных — модифицируется только потоком-владельцем
        private readonly Dictionary<Type, Dictionary<int, LocalModel>> m_dataStore = new();
        private readonly object m_dataLock = new(); // короткие локации на чтение/запись

        private List<LocalModel> m_newDataBlocks = new(); // Временное хранилище для внедрения зависимостей

        // Очереди изменений из других потоков
        private readonly ConcurrentQueue<DataAddRequest> m_incomingData = new();
        private readonly ConcurrentQueue<DataRemoveRequest> m_outgoingRemovals = new();

        //Очередь Data, которые нужно обновить
        private readonly Queue<LocalModel> m_dataToUpdate = new();

        public bool HasIncomingModels => !m_incomingData.IsEmpty;

        // Вызывается из основного потока объекта. В начале каждого апдейта
        internal void PrepareIncomingModels()
        {
            // Применяем добавления/замены данных
            while (m_incomingData.TryDequeue(out var item))
            {
                lock (m_dataLock)
                {
                    item.Data.Initialize(this);
                    if (!m_dataStore.TryGetValue(item.Type, out var byKey))
                    {
                        byKey = new Dictionary<int, LocalModel>();
                        m_dataStore[item.Type] = byKey;
                    }
                    // Публикуем заменой ссылки
                    byKey[item.Key] = item.Data;

                    InjectDependenciesIntoObject(item.Data);
                    m_newDataBlocks.Add(item.Data);
                }
            }

            lock (m_incomingData)
            {
                if (m_incomingData.Count == 0)
                {
                    m_workflow.CallRegistry.Unregister(this, MethodType.PrepareModel);
                }
                m_workflow.CallRegistry.Register(this, MethodType.OnAttachModel);
            }
        }

        internal void CallOnAttachModels()
        {
            lock (m_dataLock)
            {
                foreach (var data in m_newDataBlocks)
                {
                    data.OnAttached();
                }
                m_newDataBlocks.Clear();
                m_workflow.CallRegistry.Unregister(this, MethodType.OnAttachModel);
            }
        }

        // Вызывается из основного потока объекта. В конце каждого апдейта
        internal void CallOnDetachModels()
        {
            // Применяем удаления данных
            while (m_outgoingRemovals.TryDequeue(out var req))
            {
                lock (m_dataLock)
                {
                    if (!m_dataStore.TryGetValue(req.Type, out var storeByKey))
                        continue;

                    if (req.AllOfType)
                    {
                        foreach (var d in storeByKey.Values)
                        {
                            d.OnDetached();
                        }
                        m_dataStore.Remove(req.Type);
                        continue;
                    }

                    if (req.Key.HasValue && storeByKey.TryGetValue(req.Key.Value, out var data))
                    {
                        data.OnDetached();
                        storeByKey.Remove(req.Key.Value);
                        if (storeByKey.Count == 0) m_dataStore.Remove(req.Type);
                    }
                }
            }

            lock (m_outgoingRemovals)
            {
                if (m_outgoingRemovals.Count == 0)
                {
                    m_workflow.CallRegistry.Unregister(this, MethodType.OnDetachModel);
                }
            }
        }

        // Generic factory that returns the concrete derived type
        public T AddModel<T>(int key = 0) where T : LocalModel, new()
        {
            return AddModel(key, new T());
        }

        /// <summary>
        /// Adds a data block instance. If data of this type already exists at key 0 it will be replaced.
        /// Thread-safe.
        /// </summary>
        public LocalModel AddModel(LocalModel data)
        {
            return AddModel<LocalModel>(0, data);
        }

        /// <summary>
        /// Adds a data block instance by key. Can store multiple sets of the same type.
        /// Thread-safe.
        /// Returns the same concrete type that was passed in.
        /// </summary>
        public T AddModel<T>(int key, T data) where T : LocalModel
        {
            if (data == null) throw new ArgumentNullException(nameof(data));
            lock (m_incomingData)
            {
                m_incomingData.Enqueue(new DataAddRequest(data.GetType(), key, data));
                m_workflow?.CallRegistry.Register(this, MethodType.PrepareModel);
            }
            return data;
        }
        /// <summary>
        /// Удаляет данные типа T (ключ 0). Потокобезопасно.
        /// </summary>
        public void RemoveModel<T>() where T : LocalModel
        {
            lock (m_outgoingRemovals)
            {
                m_outgoingRemovals.Enqueue(new DataRemoveRequest(typeof(T), 0, allOfType: false));
                m_workflow?.CallRegistry.Register(this, MethodType.OnDetachModel);
            }
        }

        /// <summary>
        /// Удаляет данные типа T по ключу. Потокобезопасно.
        /// </summary>
        public void RemoveModel<T>(int key) where T : LocalModel
        {
            lock (m_outgoingRemovals)
            {
                m_outgoingRemovals.Enqueue(new DataRemoveRequest(typeof(T), key, allOfType: false));
                m_workflow?.CallRegistry.Register(this, MethodType.OnDetachModel);
            }
        }

        /// <summary>
        /// Удаляет все данные типа T. Потокобезопасно.
        /// </summary>
        public void RemoveAllModel<T>() where T : LocalModel
        {
            lock (m_outgoingRemovals)
            {
                m_outgoingRemovals.Enqueue(new DataRemoveRequest(typeof(T), key: null, allOfType: true));
                m_workflow?.CallRegistry.Register(this, MethodType.OnDetachModel);
            }
        }

        /// <summary>
        /// Возвращает данные типа T (ключ 0). Если не поток-владелец — возвращает копию.
        /// </summary>
        public bool TryGetModel<T>(int key, out T result) where T : class
        {
            result = GetModel<T>(key);
            return result != null;
        }

        public bool TryGetModel<T>(out T result) where T : class
        {
            return TryGetModel(0, out result);
        }

        /// <summary>
        /// Возвращает данные типа T по ключу. Если не поток-владелец — возвращает копию.
        /// </summary>
        public T GetModel<T>(int key = 0) where T : class
        {
            var requestedType = typeof(T);
            LocalModel value = null;

            lock (m_dataLock)
            {
                // 1. Сначала ищем по точному типу (быстро)
                if (m_dataStore.TryGetValue(requestedType, out var byKey) &&
                    byKey.TryGetValue(key, out var v))
                {
                    value = v;
                }
                else
                {
                    // 2. Если не нашли — ищем среди всех наследников
                    foreach (var (storedType, dict) in m_dataStore)
                    {
                        if (requestedType.IsAssignableFrom(storedType) && dict.TryGetValue(key, out var candidate))
                        {
                            value = candidate;
                            break;
                        }
                    }
                }
            }

            if (value == null) return default;

            if (IsCurrentThreadOwner())
                return value as T;

            return value.GetClone() as T;
        }

        /// <summary>
        /// Возвращает все данные, совместимые с T (включая наследников/интерфейсы).
        /// Для чужих потоков — возвращает копии.
        /// </summary>
        public List<T> GetAllModel<T>() where T : class
        {
            bool isOwner = IsCurrentThreadOwner();
            var result = new List<T>();

            lock (m_dataLock)
            {
                foreach (var byKey in m_dataStore.Values)
                {
                    foreach (var v in byKey.Values)
                    {
                        if (v is T t)
                        {
                            result.Add(isOwner ? t : v.GetClone() as T);
                        }
                    }
                }
            }

            return result;
        }

        //Вызывается каждый кадр в потоке объекта
        internal void CallUpdateModels()
        {
            foreach (var data in m_dataToUpdate)
            {
                data.UpdateData();
            }
            m_dataToUpdate.Clear();
            m_workflow.CallRegistry.Unregister(this, MethodType.UpdateModels);
        }

        /// <summary>
        /// Ставит в очередь на обоновление данные. Потокобезопасно.
        /// </summary>
        /// <param name="data"></param>
        internal void EnqueueForUpdate(LocalModel data)
        {
            if (m_dataToUpdate.Contains(data)) return;
            m_dataToUpdate.Enqueue(data);
            m_workflow.CallRegistry.Register(this, MethodType.UpdateModels);
        }

        internal void ScheduleDestroyModels()
        {
            lock (m_outgoingRemovals)
            {
                foreach (var type in m_dataStore.Keys)
                {
                    m_outgoingRemovals.Enqueue(new DataRemoveRequest(type, key: null, allOfType: true));
                }
                m_workflow?.CallRegistry.Register(this, MethodType.OnDetachModel);
            }
        }
    }
}