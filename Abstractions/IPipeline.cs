using System.Threading.Tasks;

namespace Abstractions
{
    public interface IPipeline<in TIn, TOut>
    {
        Task<PipelineResult<TOut>> Execute(TIn input);
    }
}