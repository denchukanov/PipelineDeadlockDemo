using System;
using System.Threading.Tasks;
using Abstractions;
using Implementation.Internal;

namespace Implementation
{
    internal sealed class TC<TInput, TOutput> : BaseTC<TInput, TOutput>
    {
        public TC(TInput input, TaskCompletionSource<PipelineResult<TOutput>> tcs)
            : base(tcs, true, null)
        {
            Input = input;
        }

        public TC(TaskCompletionSource<PipelineResult<TOutput>> tcs, Exception exception)
            : base(tcs, false, exception)
        {
            Input = default(TInput);
        }

        public TC(TInput input, TaskCompletionSource<PipelineResult<TOutput>> tcs, Exception exception)
            : base(tcs, false, exception)
        {
            Input = input;
        }

        public TInput Input { get; }
    }
}
