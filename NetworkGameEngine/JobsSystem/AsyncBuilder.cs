using System;
using System.Runtime.CompilerServices;

namespace NetworkGameEngine.JobsSystem
{
    public struct AsyncValueBuilder<T>
    {
        public static AsyncValueBuilder<T> Create() => new AsyncValueBuilder<T>(new Job<T>());

        private readonly Job<T> _awaiter;

        public AsyncValueBuilder(Job<T> awaiter)
        {
            _awaiter = awaiter;
        }


        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        public void SetException(Exception exception)
        {
            _awaiter.ThrowException(exception);
        }

        public void SetResult(T result)
        {
            _awaiter.Complete(result);  
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public Job<T> Task => _awaiter;
    }

    public struct AsyncBuilder
    {
        public static AsyncBuilder Create() => new AsyncBuilder(new Job());

        private readonly Job _awaiter;

        public AsyncBuilder(Job awaiter)
        {
        _awaiter = awaiter;
        }

        public void Start<TStateMachine>(ref TStateMachine stateMachine) where TStateMachine : IAsyncStateMachine
        {
            stateMachine.MoveNext();
        }

        public void SetStateMachine(IAsyncStateMachine stateMachine)
        {
        }

        public void SetException(Exception exception)
        {
            _awaiter.ThrowException(exception);
        }

        public void SetResult()
        {
            _awaiter.Complete();
        }

        public void AwaitOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : INotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public void AwaitUnsafeOnCompleted<TAwaiter, TStateMachine>(ref TAwaiter awaiter, ref TStateMachine stateMachine) where TAwaiter : ICriticalNotifyCompletion where TStateMachine : IAsyncStateMachine
        {
            awaiter.OnCompleted(stateMachine.MoveNext);
        }

        public Job Task => _awaiter;
    }
}