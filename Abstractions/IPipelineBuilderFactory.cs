using System.Threading;

namespace Abstractions
{
    public interface IPipelineBuilderFactory
    {
        IPipelineBuilder<TIn, TOut> NewBuilder<TIn, TOut>();
    }
}
