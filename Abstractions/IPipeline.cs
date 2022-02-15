using System.Threading;
using System.Threading.Tasks;

namespace Abstractions
{
    public interface IPipeline<in TIn, TOut>
    {
        Task<PipelineResult<TOut>> Execute(TIn input);
        Task<PipelineResult<TOut>> Execute(TIn input, CancellationToken cancellationToken);
    }
}
