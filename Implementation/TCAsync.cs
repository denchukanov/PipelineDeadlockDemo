using System;
using System.Threading.Tasks;
using Abstractions;
using Implementation.Internal;

namespace Implementation
{
    internal sealed class TCAsync<TInput, TOutput> : BaseTC<TInput, TOutput>
    {
        public TCAsync(Task<TInput> input, TaskCompletionSource<PipelineResult<TOutput>> tcs)
            : base(tcs, true, null)
        {
            Input = input;
        }


        public TCAsync(TaskCompletionSource<PipelineResult<TOutput>> tcs, Exception exception)
            : base(tcs, false, exception)
        {
            Input = Task.FromResult(default(TInput));
        }

        public TCAsync(Task<TInput> input, TaskCompletionSource<PipelineResult<TOutput>> tcs, Exception exception)
            : base(tcs, false, exception)
        {
            Input = input;
        }

        public Task<TInput> Input { get; }
    }
}
