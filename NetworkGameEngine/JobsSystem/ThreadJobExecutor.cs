using System.Diagnostics;

namespace NetworkGameEngine.JobsSystem
{
    public class ThreadJobExecutor
    {
        private int m_threadID;
        private LinkedList<IJob> m_jobs = new LinkedList<IJob>();

        public ThreadJobExecutor(int thId)
        {
            m_threadID = thId;
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
                if (node.Value.TryFinalize())
                {
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
