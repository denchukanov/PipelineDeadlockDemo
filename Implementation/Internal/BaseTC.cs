using System;
using System.Threading.Tasks;
using Abstractions;

namespace Implementation.Internal
{
    internal abstract class BaseTC<TInput, TOutput>
    {
        public TaskCompletionSource<PipelineResult<TOutput>> TaskCompletionSource { get; }
        public bool IsSuccess { get; }
        public Exception Exception { get; }

        protected BaseTC(TaskCompletionSource<PipelineResult<TOutput>> taskCompletionSource,
            bool isSuccess,
            Exception exception)
        {
            TaskCompletionSource = taskCompletionSource;
            IsSuccess = isSuccess;
            Exception = exception;
        }
    }
}
