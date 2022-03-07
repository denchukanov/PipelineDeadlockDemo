using System;
using System.Threading.Tasks;
using Abstractions;
using Implementation.Internal;

namespace Implementation
{
    internal sealed class SyncStepContext<TInput, TOutput> : PipelineExecutionContext<TOutput>
    {
        public SyncStepContext(TInput input, TaskCompletionSource<PipelineResult<TOutput>> tcs)
            : base(tcs, true, null)
        {
            Input = input;
        }

        public SyncStepContext(TaskCompletionSource<PipelineResult<TOutput>> tcs, Exception exception)
            : base(tcs, false, exception)
        {
            Input = default(TInput);
        }

        public TInput Input { get; }
    }
}
