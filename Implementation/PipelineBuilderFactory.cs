using System.Threading;
using Abstractions;

namespace Implementation
{
    public class PipelineBuilderFactory : IPipelineBuilderFactory
    {
        public IPipelineBuilder<TIn, TOut> NewBuilder<TIn, TOut>(CancellationTokenSource cts = null)
        {
            return new PipelineBuilder<TIn, TOut>(cts);
        }
    }
}