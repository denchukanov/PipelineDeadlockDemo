using System;
using System.Threading;
using System.Threading.Tasks;

namespace Abstractions
{
    public interface IPipelineBuilder<in TIn, TOut>
    {
        IPipelineBuilder<TIn, TOut> AddStep<TStepIn, TStepOut>(Func<TStepIn, TStepOut> stepFn);
        IPipelineBuilder<TIn, TOut> AddStepAsync<TStepIn, TStepOut>(Func<TStepIn, Task<TStepOut>> stepFn);
        IPipeline<TIn, TOut> Create(CancellationTokenSource cts = null);
    }
}