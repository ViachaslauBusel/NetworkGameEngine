using System.Diagnostics;

namespace NetworkGameEngine.JobsSystem
{
    internal struct JobEntry
    {
        public readonly IJob Job;
        public readonly GameObject Owner;

        public JobEntry(IJob job, GameObject owner)
        {
            Job = job;
            Owner = owner;
        }
    }
    internal class ThreadJobExecutor
    {
        private int m_threadID;
        private World m_world;
        private Workflow m_workflow;
        private LinkedList<JobEntry> m_jobs = new LinkedList<JobEntry>();

        public ThreadJobExecutor(int thId, World world, Workflow workflow)
        {
            m_threadID = thId;
            m_world = world;
            m_workflow = workflow;
        }

        public void Update()
        {
            Debug.Assert(m_threadID == Thread.CurrentThread.ManagedThreadId,
                "Was called by a thread that does not own this data");

            // Remove all completed jobs
            LinkedListNode<JobEntry> node = m_jobs.First;
            while (node != null)
            {
                LinkedListNode<JobEntry> next = node.Next;
                try
                {
                    m_workflow.SetCurrentGameObject(node.Value.Owner);
                    if (node.Value.Job.TryFinalize())
                    {
                        m_jobs.Remove(node);
                    }
                }
                catch (Exception ex)
                {
                    m_world.LogError("Exception during job finalization: " + ex);
                    m_jobs.Remove(node);
                }
                node = next;
            }
            m_workflow.SetCurrentGameObject(null);
            // m_jobs.RemoveWhere((j) => j.TryFinalize());
        }

        internal void AddJob(IJob job)
        {
            m_jobs.AddFirst(new JobEntry(job, m_workflow.CurrentGameObject));
        } 
    }
}
