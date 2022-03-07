using Abstractions;

namespace Implementation
{
    public class PipelineBuilderFactory : IPipelineBuilderFactory
    {
        public IPipelineBuilder<TIn, TOut> NewBuilder<TIn, TOut>()
        {
            return new PipelineBuilder<TIn, TOut>();
        }
    }
}
