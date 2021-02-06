using System.Threading.Tasks.Dataflow;
using Abstractions;

namespace Implementation
{
    internal class DataflowStep : IStep
    {
        private DataflowStep(IDataflowBlock block, bool isAsync)
        {
            Block = block;
            IsAsync = isAsync;
        }

        public IDataflowBlock Block { get; }
        public bool IsAsync { get; }

        public static IStep Async(IDataflowBlock block) => new DataflowStep(block, true);
        public static IStep Sync(IDataflowBlock block) => new DataflowStep(block, false);
    }
}