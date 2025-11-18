using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NetworkGameEngine.JobsSystem
{
    public partial class Job
    {
        public static MillisDelayJob Delay(int millis)
        {
            return new MillisDelayJob(millis);
        }

        public static SecondsDelayJob Delay(float seconds)
        {
            return new SecondsDelayJob(seconds);
        }

        public static WaitUntilJob WaitUntil(Func<bool> predicate)
        {
            return new WaitUntilJob(predicate);
        }

        public static WaitWhileJob WaitWhile(Func<bool> predicate)
        {
            return new WaitWhileJob(predicate);
        }

        // Lightweight bridge from Task to Job without Task.Run or async state machines
        public static Job Wait(Task task)
        {
            var job = new Job();

            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                {
                    job.ThrowException(task.Exception?.InnerException ?? task.Exception!);
                }
                else if (task.IsCanceled)
                {
                    job.ThrowException(new TaskCanceledException(task));
                }
                else
                {
                    job.Complete();
                }
                return job;
            }

            task.ContinueWith(static (t, s) =>
            {
                var j = (Job)s!;
                if (t.IsFaulted)
                {
                    j.ThrowException(t.Exception?.InnerException ?? t.Exception!);
                }
                else if (t.IsCanceled)
                {
                    j.ThrowException(new TaskCanceledException(t));
                }
                else
                {
                    j.Complete();
                }
            }, job, CancellationToken.None,
               TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.HideScheduler,
               TaskScheduler.Default);

            return job;
        }

        // Lightweight bridge from Task<T> to Job<T> without Task.Run or async state machines
        public static Job<T> Wait<T>(Task<T> task)
        {
            var job = new Job<T>();

            if (task.IsCompleted)
            {
                if (task.IsFaulted)
                {
                    job.ThrowException(task.Exception?.InnerException ?? task.Exception!);
                }
                else if (task.IsCanceled)
                {
                    job.ThrowException(new TaskCanceledException(task));
                }
                else
                {
                    job.Complete(task.Result);
                }
                return job;
            }

            task.ContinueWith(static (t, s) =>
            {
                var j = (Job<T>)s!;
                if (t.IsFaulted)
                {
                    j.ThrowException(t.Exception?.InnerException ?? t.Exception!);
                }
                else if (t.IsCanceled)
                {
                    j.ThrowException(new TaskCanceledException(t));
                }
                else
                {
                    j.Complete(t.Result);
                }
            }, job, CancellationToken.None,
               TaskContinuationOptions.ExecuteSynchronously | TaskContinuationOptions.DenyChildAttach | TaskContinuationOptions.HideScheduler,
               TaskScheduler.Default);

            return job;
        }

        /// <summary>
        /// Run CPU work on the thread pool with minimal overhead (no ExecutionContext flow), returning a Job.
        /// Prefer this over Task.Run when you only need to await completion inside the engine.
        /// </summary>
        public static Job Run(Action action)
        {
            var job = new Job();
            ThreadPool.UnsafeQueueUserWorkItem(static s =>
            {
                var (act, j) = ((Action, Job))s!;
                try
                {
                    act();
                    j.Complete();
                }
                catch (Exception ex)
                {
                    j.ThrowException(ex);
                }
            }, (action, job), preferLocal: true);
            return job;
        }

        /// <summary>
        /// Run CPU work on the thread pool with minimal overhead (no ExecutionContext flow), returning a Job with a result.
        /// </summary>
        public static Job<T> Run<T>(Func<T> func)
        {
            var job = new Job<T>();
            ThreadPool.UnsafeQueueUserWorkItem(static s =>
            {
                var (fn, j) = ((Func<T>, Job<T>))s!;
                try
                {
                    var res = fn();
                    j.Complete(res);
                }
                catch (Exception ex)
                {
                    j.ThrowException(ex);
                }
            }, (func, job), preferLocal: true);
            return job;
        }
    }
}
