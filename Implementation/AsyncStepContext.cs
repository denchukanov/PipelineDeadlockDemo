using System;
using System.Threading.Tasks;
using Abstractions;
using Implementation.Internal;

namespace Implementation
{
    internal sealed class AsyncStepContext<TInput, TOutput> : PipelineExecutionContext<TOutput>
    {
        public AsyncStepContext(Task<TInput> input, TaskCompletionSource<PipelineResult<TOutput>> tcs)
            : base(tcs, true, null)
        {
            Input = input;
        }


        public AsyncStepContext(TaskCompletionSource<PipelineResult<TOutput>> tcs, Exception exception)
            : base(tcs, false, exception)
        {
            Input = Task.FromResult(default(TInput));
        }

        public Task<TInput> Input { get; }
    }
}
