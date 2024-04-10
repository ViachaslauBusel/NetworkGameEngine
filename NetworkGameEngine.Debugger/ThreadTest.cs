using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NetworkGameEngine.UnitTests
{
    internal class ThreadTest
    {
        [Test]
        public void TestTH()
        {
            Console.WriteLine($"ThreadID:{Thread.CurrentThread.ManagedThreadId}");
            TestTH2();
            Console.WriteLine($"END:ThreadID:{Thread.CurrentThread.ManagedThreadId}");
            Thread.Sleep(2_000);
        }


        public async void TestTH2()
        {
            Console.WriteLine($"ThreadID:{Thread.CurrentThread.ManagedThreadId}");
            await RunTask();
            Console.WriteLine($"ThreadID:{Thread.CurrentThread.ManagedThreadId}");
        }

        public async Task RunTask()
        {
            Thread.Sleep(1_000);
            Console.WriteLine($"ThreadID:{Thread.CurrentThread.ManagedThreadId}");
            await RunTask2();
        }

        private async Task RunTask2()
        {
            Console.WriteLine($"ThreadID:{Thread.CurrentThread.ManagedThreadId}");
            Thread.Sleep(11);
        }
    }
}
