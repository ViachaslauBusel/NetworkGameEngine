namespace NetworkGameEngine.Workflows
{
    internal class WorkflowPool
    {
        private Workflow[] m_workflowArray;
        private World m_world;

        public int Count => m_workflowArray.Length;
        public IReadOnlyCollection<Workflow> AllWorkflows => m_workflowArray;

        public WorkflowPool(World world)
        {
            m_world = world;
        }

        internal void Init(int maxThread)
        {
            m_workflowArray = new Workflow[maxThread];
            for (int i = 0; i < m_workflowArray.Length; i++)
            {
                m_workflowArray[i] = new Workflow();
                m_workflowArray[i].Init(m_world);
                GlobalWorkflowRegistry.RegisterWorkflow(m_workflowArray[i]);
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

        internal Workflow GetWorkflowByThreadID(int threadID) => GlobalWorkflowRegistry.GetWorkflow(threadID);

        internal Workflow GetCurrentWorkflow() => GlobalWorkflowRegistry.GetCurrentWorkflow();
    }
}
