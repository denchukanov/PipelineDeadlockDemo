using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Abstractions;

namespace Implementation
{
    internal class Pipeline<TIn, TOut> : IPipeline<TIn, TOut>
    {
        private readonly IEnumerable<IStep> _steps;

        public Pipeline(IEnumerable<IStep> steps)
        {
            if (steps is null) throw new ArgumentNullException(nameof(steps));
            if (steps.FirstOrDefault() is null) throw new ArgumentException();

            _steps = steps;
        }

        public Task<PipelineResult<TOut>> Execute(TIn input)
        {
            var firstStep = (_steps.First() as DataflowStep).Block as ITargetBlock<TC<TIn, TOut>>;
            var tcs = new TaskCompletionSource<PipelineResult<TOut>>();
            firstStep.SendAsync(new TC<TIn, TOut>(input, tcs));
            return tcs.Task;
        }
    }
}