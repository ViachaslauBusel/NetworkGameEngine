using System.Diagnostics;

namespace NetworkGameEngine.JobsSystem
{
    public class ThreadJobExecutor
    {
        private int m_threadID;
        private World m_world;
        private LinkedList<IJob> m_jobs = new LinkedList<IJob>();

        public ThreadJobExecutor(int thId, World world)
        {
            m_threadID = thId;
            m_world = world;
        }

        public void Update()
        {

            Debug.Assert(m_threadID == Thread.CurrentThread.ManagedThreadId,
                "Was called by a thread that does not own this data");


            // Remove all completed jobs
            LinkedListNode<IJob> node = m_jobs.First;
            while (node != null)
            {
                LinkedListNode<IJob> next = node.Next;
                try
                {
                    if (node.Value.TryFinalize())
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
            // m_jobs.RemoveWhere((j) => j.TryFinalize());
        }

        internal void AddJob(IJob job)
        {
            m_jobs.AddFirst(job);
        } 
    }
}
