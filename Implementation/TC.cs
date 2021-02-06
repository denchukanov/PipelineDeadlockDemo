using System;
using System.Threading.Tasks;
using Abstractions;

namespace Implementation
{
    internal class TC<TInput, TOutput>
    {
        public TC(TInput input, TaskCompletionSource<PipelineResult<TOutput>> tcs)
        {
            Input = input;
            TaskCompletionSource = tcs;
            Exception = null;
            IsSuccess = true;
        }

        public TC(TaskCompletionSource<PipelineResult<TOutput>> tcs, Exception exception)
        {
            Input = default(TInput);
            TaskCompletionSource = tcs;
            Exception = exception;
            IsSuccess = false;
        }

        public TC(TInput input, TaskCompletionSource<PipelineResult<TOutput>> tcs, Exception exception)
        {
            Input = input;
            TaskCompletionSource = tcs;
            Exception = exception;
            IsSuccess = false;
        }

        public TInput Input { get; }
        public TaskCompletionSource<PipelineResult<TOutput>> TaskCompletionSource { get; }
        public bool IsSuccess { get; }
        public Exception Exception { get; }
    }
}