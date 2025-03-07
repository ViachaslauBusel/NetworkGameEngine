﻿using System.Collections.Concurrent;

namespace NetworkGameEngine.JobsSystem
{

    internal static class JobsManager
    {
        private static ConcurrentDictionary<int, ThreadJobExecutor> m_jobsExecutor = new ConcurrentDictionary<int, ThreadJobExecutor>();

        internal static ThreadJobExecutor RegisterThreadHandler()
        {
            int thID = Thread.CurrentThread.ManagedThreadId;
            if (m_jobsExecutor.TryAdd(thID, new ThreadJobExecutor(thID)))
            {
                return m_jobsExecutor[thID];
            }
            throw new Exception("JobsSystem: Attempt to re-register a handler thread");
        }

        internal static void RegisterAwaiter(IJob job)
        {
            int thID = Thread.CurrentThread.ManagedThreadId; 
            if(m_jobsExecutor.TryGetValue(thID, out ThreadJobExecutor jobExecutor))
            {
                jobExecutor.AddJob(job);
            }
            else throw new Exception("JobsSystem: this thread is not registered for awaiter processing");
        }
    }
}
