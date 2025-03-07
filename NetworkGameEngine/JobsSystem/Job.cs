using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace NetworkGameEngine.JobsSystem
{
    [AsyncMethodBuilder(typeof(AsyncBuilder))]
    public partial class Job : IJob, INotifyCompletion
    {
        private Action _continuation;
        private bool _isCompleted = false;
        private Exception _exception;
        private readonly object _lock = new object();

        public virtual bool IsCompleted => _isCompleted;
        public bool IsFaulted => _exception != null;
        public Exception Exception => _exception;
        public bool GetResult() => _isCompleted;
        public Job GetAwaiter() => this;

        public Job()
        {
            JobsManager.RegisterAwaiter(this);
        }

        public void OnCompleted(Action continuation)
        {
            _continuation += continuation;
        }

        public void ThrowException(Exception e)
        {
            lock (_lock)
            {
                _exception = e;
                _isCompleted = true;
                Monitor.PulseAll(_lock);
            }
        }

        public void Complete()
        {
            lock (_lock)
            {
                _isCompleted = true;
                Monitor.PulseAll(_lock);
            }
        }

        public void Wait(int waitTime)
        {
            lock (_lock)
            {
                while (!IsCompleted)
                {
                    Monitor.Wait(_lock, waitTime);
                }
            }
        }

        public void Wait()
        {
            lock (_lock)
            {
                while (!IsCompleted)
                {
                    Monitor.Wait(_lock);
                }
            }
        }

        public bool TryFinalize()
        {
            if (IsCompleted)
            {
                _continuation?.Invoke();
                _continuation = null;
                return true;
            }
            return false;
        }

        public static Job WhenAll(IEnumerable<IJob> jobs)
        {
            return Job.Wait(Task.Run(() =>
            {
                foreach (var job in jobs)
                {
                    job.Wait();
                }
            }));
        }

        internal static Job<T> FromException<T>(Exception ex)
        {
            throw new NotImplementedException();
        }
    }

    [AsyncMethodBuilder(typeof(AsyncValueBuilder<>))]
    public class Job<T> : IJob, INotifyCompletion
    {
        private Action _continuation;
        private bool _isCompleted = false;
        private T _result;
        private Exception _exception;
        private readonly object _lock = new object();

        public bool IsCompleted => _isCompleted;
        public bool IsFaulted => _exception != null;
        public Exception Exception => _exception;

        public Job<T> GetAwaiter() => this;

        public Job()
        {
            JobsManager.RegisterAwaiter(this);
        }

        public T GetResult()
        {
            if (!_isCompleted) throw new Exception("Job is not completed yet");
            if (_exception != null && _exception is not TimeoutException) ExceptionDispatchInfo.Throw(_exception);

            return _result;
        }

        public void OnCompleted(Action continuation)
        {
            _continuation += continuation;
        }

        public void Wait()
        {
            lock (_lock)
            {
                while (!IsCompleted)
                {
                    Monitor.Wait(_lock);
                }
            }
        }

        public void Wait(int waitTime)
        {
            lock (_lock)
            {
                while (!IsCompleted)
                {
                    Monitor.Wait(_lock, waitTime);
                }
            }
        }

        public void ThrowException(Exception e)
        {
            lock (_lock)
            {
                _exception = e;
                _isCompleted = true;
                Monitor.PulseAll(_lock);
            }
        }

        public void Complete(T result)
        {
            lock (_lock)
            {
                _result = result;
                _isCompleted = true;
                Monitor.PulseAll(_lock);
            }
        }

        public bool TryFinalize()
        {
            if (IsCompleted)
            {
                _continuation?.Invoke();
                _continuation = null;
                return true;
            }
            return false;
        }
    }
}

