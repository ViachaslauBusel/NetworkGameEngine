using NetworkGameEngine.JobsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.UnitTests
{
    internal class TestJobsSystem
    {
        private static volatile int m_part = 0;

        [Test]
        public void TestAwaiter()
        {
            ThreadJobExecutor job = JobsManager.RegisterThreadHandler();

            TestAwaiter2();
            Assert.AreEqual(++m_part, 2);
            Console.WriteLine("TestAwaiter2:part_2");
            while(m_part != 3)
            {
                job.Update();
                Thread.Sleep(10);
            }
        }

        private async void TestAwaiter2()
        {
            Console.WriteLine("TestAwaiter2:part_0");
            Assert.AreEqual(++m_part, 1);
            int threadID = Thread.CurrentThread.ManagedThreadId;
            await JobsManager.Execute(Task.Delay(100));
            Assert.AreEqual(++m_part, 3);
            Console.WriteLine("TestAwaiter2:part_3");
            Assert.AreEqual(threadID, Thread.CurrentThread.ManagedThreadId);
        }

        [Test]
        public void TestWhenAll()
        {
            ThreadJobExecutor job = JobsManager.RegisterThreadHandler();
            m_part = 0;
            TestWhenAll2();
            while (m_part != 3)
            {
                job.Update();
                Thread.Sleep(10);
            }
        }

        private async void TestWhenAll2()
        {
            Console.WriteLine("1");
            Assert.AreEqual(++m_part, 1);
            await JobsManager.Execute(Task.Delay(100));
            Console.WriteLine("2");
            Assert.AreEqual(++m_part, 2);
            List<Job> jobs = new List<Job>();

            for (int i = 0; i < 10; i++)
            {
                jobs.Add(JobsManager.Execute(Task.Delay(1_000)));
            }

            long start = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            await JobsManager.Execute(Task.Delay(2_000));
            long end = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            Console.WriteLine($"Time:{end - start}");

            await Job.WhenAll(jobs);
            Console.WriteLine("3");
            Assert.AreEqual(++m_part, 3);
        }

        // Тест на создание Job внутри Job
        private int m_threadID;
        [Test]
        public void TestCreateJobInJob()
        {
            ThreadJobExecutor job = JobsManager.RegisterThreadHandler();
            m_threadID = Thread.CurrentThread.ManagedThreadId;
            m_part = 0;
            TestCreateJobInJob2();
            while (m_part != 4)
            {
                job.Update();
                Thread.Sleep(10);
            }
        }

        private async Job TestCreateJobInJob2()
        {
            Assert.AreEqual(0, m_part++);
            Assert.AreEqual(m_threadID, Thread.CurrentThread.ManagedThreadId);
            await TestCreateJobInJob3();
            Assert.AreEqual(m_threadID, Thread.CurrentThread.ManagedThreadId);
            Assert.AreEqual(3, m_part++);
        }

        private async Job TestCreateJobInJob3()
        {
            Assert.AreEqual(1, m_part++);
            Assert.AreEqual(m_threadID, Thread.CurrentThread.ManagedThreadId);
            await JobsManager.Execute(Task.Delay(100));
            Assert.AreEqual(2, m_part++);
            Assert.AreEqual(m_threadID, Thread.CurrentThread.ManagedThreadId);
        }
    }
}
