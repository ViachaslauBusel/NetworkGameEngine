namespace NetworkGameEngine.Workflows
{
    internal static class GlobalWorkflowRegistry
    {
        private static Dictionary<int, Workflow> m_workflows = new Dictionary<int, Workflow>();

        internal static void RegisterWorkflow(Workflow workflow)
        {
            if (m_workflows.ContainsKey(workflow.ThreadID))
            {
                throw new InvalidOperationException($"A workflow with Thread ID {workflow.ThreadID} is already registered.");
            }
            m_workflows[workflow.ThreadID] = workflow;
        }

        internal static Workflow GetWorkflowByThreadId(int threadID)
        {
            m_workflows.TryGetValue(threadID, out var workflow);
            return workflow;
        }

        internal static void UnregisterWorkflow(int threadID)
        {
            m_workflows.Remove(threadID);
        }

        internal static Workflow GetCurrentWorkflow()
        {
            var threadID = Thread.CurrentThread.ManagedThreadId;
            return GetWorkflowByThreadId(threadID);
        }
    }
}
