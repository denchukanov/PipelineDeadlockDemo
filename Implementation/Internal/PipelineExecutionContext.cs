using System;
using System.Threading.Tasks;
using Abstractions;

namespace Implementation.Internal
{
    
    internal abstract class PipelineExecutionContext<TOut>
    {
        public TaskCompletionSource<PipelineResult<TOut>> TaskCompletionSource { get; }
        public bool IsSuccess { get; }
        public Exception Exception { get; }

        protected PipelineExecutionContext(TaskCompletionSource<PipelineResult<TOut>> taskCompletionSource,
            bool isSuccess,
            Exception exception)
        {
            TaskCompletionSource = taskCompletionSource;
            IsSuccess = isSuccess;
            Exception = exception;
        }
    }
}
