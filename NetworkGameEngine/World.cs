using NetworkGameEngine.Tools;
using System.Collections.Concurrent;
using Zenject;

namespace NetworkGameEngine
{
    public class AddingObjectTask : TaskBase
    {
       public GameObject GameObject { get; set; }
        public int GameObjectID { get; set; } = 0;
    }
    public class RemovingObjectTask : TaskBase
    {
        public int GameObjectID { get; set; }
    }
    public class World
    {
        private DiContainer m_diContainer = new DiContainer();
        private ConcurrentDictionary<int, GameObject> m_objects = new ConcurrentDictionary<int, GameObject>();
        private ConcurrentQueue<AddingObjectTask> m_addObjects = new ConcurrentQueue<AddingObjectTask>();
        private ConcurrentQueue<RemovingObjectTask> m_removeObjects = new ConcurrentQueue<RemovingObjectTask>();
        private List<GameObject> m_removedObjects = new List<GameObject>();
        private int m_generatorID = 1;
        private Workflow[] m_workflows;
        private int m_addObjectThIndex = 0;

        internal DiContainer DiContainer => m_diContainer;

        public void RegisterService<T>(T service)
        {
            m_diContainer.Bind<T>().FromInstance(service).AsSingle();
        }


        public void Init(int maxThread)
        {
            m_workflows = new Workflow[maxThread];
            for (int i = 0; i < m_workflows.Length; i++)
            {
                m_workflows[i] = new Workflow();
                m_workflows[i].Init();
            }
        }
      
        public async Task<int> AddGameObject(GameObject obj)
        {
            var task = new AddingObjectTask() { GameObject = obj };
            m_addObjects.Enqueue(task);
            await task.Wait();

            return task.GameObjectID;
        }

        public void RemoveGameObject(int gameObjectID) 
        {
            m_removeObjects.Enqueue(new RemovingObjectTask() { GameObjectID = gameObjectID });
        }

        public void Update()
        {
            int addObjectsCount = m_addObjects.Count;
            for (int i = 0; i < addObjectsCount && m_addObjects.TryDequeue(out var task); i++)
            {
                GameObject obj = task.GameObject;
                obj.Init(m_generatorID++, m_workflows[m_addObjectThIndex].ThreadID, this);

                m_objects.TryAdd(obj.ID, obj);

                m_workflows[m_addObjectThIndex].AddObject(obj);
                m_addObjectThIndex = (m_addObjectThIndex + 1) % m_workflows.Length;

                task.GameObjectID = obj.ID;
                task.Completed(true);
            }

            foreach (var th in m_workflows) { th.CallMethod(MethodType.Prepare); }
            foreach (var th in m_workflows) { th.Wait(); }

            foreach (var th in m_workflows) { th.CallMethod(MethodType.Init); }
            foreach (var th in m_workflows) { th.Wait(); }

            foreach (var th in m_workflows) { th.CallMethod(MethodType.Start); }
            foreach (var th in m_workflows) { th.Wait(); }

            foreach (var th in m_workflows) { th.CallMethod(MethodType.Update); }
            foreach (var th in m_workflows) { th.Wait(); }

            foreach (var th in m_workflows) { th.CallMethod(MethodType.Command); }
            foreach (var th in m_workflows) { th.Wait(); }

            foreach (var th in m_workflows) { th.CallMethod(MethodType.LateUpdate); }
            foreach (var th in m_workflows) { th.Wait(); }

            int removeObjectsCount = m_removeObjects.Count;
            for (int i = 0; i < removeObjectsCount && m_removeObjects.TryDequeue(out var task); i++)
            {
                bool isRemoved = m_objects.ContainsKey(task.GameObjectID);
                if (isRemoved)
                {
                    GameObject removeObj = m_objects[task.GameObjectID];
                    m_removedObjects.Add(removeObj);

                    removeObj.Destroy();
                }
                task.Completed(isRemoved);
            }
            foreach (var th in m_workflows) { th.CallMethod(MethodType.OnDestroy); }
            foreach (var th in m_workflows) { th.Wait(); }

            foreach (var th in m_workflows) { th.CallMethod(MethodType.UpdateData); }
            foreach (var th in m_workflows) { th.Wait(); }

            foreach (var obj in m_removedObjects)
            {
                m_objects.TryRemove(obj.ID, out _);
                m_workflows.First(th => th.ThreadID == obj.ThreadID).RemoveObject(obj);
            }
            m_removedObjects.Clear();
        }

        public bool TryGetGameObject(int objectID, out GameObject obj) => m_objects.TryGetValue(objectID, out obj);
    }
}
