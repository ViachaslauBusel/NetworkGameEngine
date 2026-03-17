using NetworkGameEngine.Interfaces;

namespace NetworkGameEngine.Workflows
{
    internal class WorkflowPool
    {
        private readonly World m_world;
        private Workflow[] m_workflowArray;
        private CountdownEvent m_barrier;
        private volatile MethodType m_scheduledMethod;
        private volatile IMultiThreadUpdatableService m_currentMultiThreadService;

        public int Count => m_workflowArray.Length;
        public MethodType ScheduledMethod => m_scheduledMethod;
        public IMultiThreadUpdatableService CurrentMultiThreadService => m_currentMultiThreadService;
        public IReadOnlyCollection<Workflow> AllWorkflows => m_workflowArray;

        public WorkflowPool(World world)
        {
            m_world = world;
        }

        internal void Init(int maxThread)
        {
            m_workflowArray = new Workflow[maxThread];
            m_barrier = new CountdownEvent(maxThread);

            for (int i = 0; i < m_workflowArray.Length; i++)
            {
                m_workflowArray[i] = new Workflow(this, m_barrier, i);
                m_workflowArray[i].Init(m_world);
                GlobalWorkflowRegistry.RegisterWorkflow(m_workflowArray[i]);
            }
        }

        public void CallMethod(MethodType method)
        {
            m_scheduledMethod = method;
            m_barrier.Reset(m_workflowArray.Length);

            for (int i = 0; i < m_workflowArray.Length; i++)
            {
                m_workflowArray[i].DispatchOnce();
            }

            m_barrier.Wait();

            m_scheduledMethod = MethodType.None;
        }

        internal void RunMultiThreadService(IMultiThreadUpdatableService service)
        {
            m_currentMultiThreadService = service;
            try
            {
                CallMethod(MethodType.MultiThreadService);
            }
            finally
            {
                m_currentMultiThreadService = null;
            }
        }

        internal Workflow GetWorkflowByIndex(int index)
        {
            if (index < 0 || index >= m_workflowArray.Length)
            {
                throw new IndexOutOfRangeException($"Index {index} is out of range for workflow array.");
            }
            return m_workflowArray[index];
        }

        internal Workflow GetWorkflowByThreadId(int threadID) => GlobalWorkflowRegistry.GetWorkflowByThreadId(threadID);

        internal Workflow GetCurrentWorkflow() => GlobalWorkflowRegistry.GetCurrentWorkflow();
    }
}
