using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Abstractions;

namespace Implementation
{
    internal class PipelineBuilder<TIn, TOut> : IPipelineBuilder<TIn, TOut>
    {
        private readonly List<IStep> _steps = new();
        private readonly ExecutionDataflowBlockOptions _options;

        public PipelineBuilder(CancellationTokenSource cts)
        {
            _options = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                // CancellationToken = cts?.Token ?? CancellationToken.None
            };
        }

        
        public IPipelineBuilder<TIn, TOut> AddStep<TStepIn, TStepOut>(Func<TStepIn, TStepOut> stepFn)
        {
            TransformBlock<TC<TStepIn, TOut>, TC<TStepOut, TOut>> DefaultSyncStep(Func<TStepIn, TStepOut> fn)
                => new((tc) =>
                    {
                        TC<TStepOut, TOut> result;
                        try
                        {
                            if (!tc.IsSuccess)
                                return new TC<TStepOut, TOut>(tc.TaskCompletionSource, tc.Exception);

                            var stepResult = fn(tc.Input);
                            result = new TC<TStepOut, TOut>(stepResult, tc.TaskCompletionSource);
                        }
                        catch (Exception e)
                        {
                            result = new TC<TStepOut, TOut>(tc.TaskCompletionSource, e);
                        }

                        return result;
                    }, _options
                );

            if (_steps.Count == 0)
            {
                var step = DefaultSyncStep(stepFn);
                _steps.Add(DataflowStep.Sync(step));
            }
            else
            {
                var lastStep = _steps.Last();

                if (lastStep.IsAsync)
                {
                    var step = new TransformBlock<TCAsync<TStepIn, TOut>, TC<TStepOut, TOut>>(async (tc) =>
                    {
                        try
                        {
                            if (!tc.IsSuccess)
                                return new TC<TStepOut, TOut>(tc.TaskCompletionSource, tc.Exception);

                            var s = await tc.Input;
                            return new TC<TStepOut, TOut>(stepFn(s), tc.TaskCompletionSource);
                        }
                        catch (Exception e)
                        {
                            return new TC<TStepOut, TOut>(tc.TaskCompletionSource, e);
                        }
                    }, _options);
                    var targetBlock = ((lastStep as DataflowStep).Block as ISourceBlock<TCAsync<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Sync(step));
                }
                else
                {
                    var step = DefaultSyncStep(stepFn);
                    var targetBlock = ((lastStep as DataflowStep).Block as ISourceBlock<TC<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Sync(step));
                }
            }

            return this;
        }

        public IPipelineBuilder<TIn, TOut> AddStepAsync<TStepIn, TStepOut>(Func<TStepIn, Task<TStepOut>> stepFn)
        {
            if (_steps.Count == 0)
            {
                var step = new TransformBlock<TC<TStepIn, TOut>, TCAsync<TStepOut, TOut>>((tc) =>
                {
                    try
                    {
                        if (!tc.IsSuccess)
                            return new TCAsync<TStepOut, TOut>(tc.TaskCompletionSource, tc.Exception);

                        return new TCAsync<TStepOut, TOut>(stepFn(tc.Input), tc.TaskCompletionSource);
                    }
                    catch (Exception e)
                    {
                        return new TCAsync<TStepOut, TOut>(tc.TaskCompletionSource, e);
                    }
                }, _options);
                _steps.Add(DataflowStep.Async(step));
            }
            else
            {
                var lastStep = _steps.Last();

                if (lastStep.IsAsync)
                {
                    var step = new TransformBlock<TCAsync<TStepIn, TOut>, TCAsync<TStepOut, TOut>>(async (tc) =>
                    {
                        try
                        {
                            if (!tc.IsSuccess)
                            {
                                return new TCAsync<TStepOut, TOut>(tc.TaskCompletionSource, tc.Exception);
                            }

                            return new TCAsync<TStepOut, TOut>(stepFn(await tc.Input), tc.TaskCompletionSource);
                        }
                        catch (Exception e)
                        {
                            return new TCAsync<TStepOut, TOut>(tc.TaskCompletionSource, e);
                        }
                    }, _options);
                    var targetBlock = ((lastStep as DataflowStep).Block as ISourceBlock<TCAsync<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Async(step));
                }
                else
                {
                    var step = new TransformBlock<TC<TStepIn, TOut>, TCAsync<TStepOut, TOut>>((tc) =>
                    {
                        try
                        {
                            if (!tc.IsSuccess)
                            {
                                return new TCAsync<TStepOut, TOut>(tc.TaskCompletionSource, tc.Exception);
                            }

                            return new TCAsync<TStepOut, TOut>(stepFn(tc.Input), tc.TaskCompletionSource);
                        }
                        catch (Exception e)
                        {
                            return new TCAsync<TStepOut, TOut>(tc.TaskCompletionSource, e);
                        }
                    }, _options);
                    var targetBlock = ((lastStep as DataflowStep).Block as ISourceBlock<TC<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Async(step));
                }
            }

            return this;
        }

        public IPipeline<TIn, TOut> Create(CancellationTokenSource cts = null)
        {
            if (_steps.Count == 0) throw new InvalidOperationException();

            var setResultStep =
                new ActionBlock<TC<TOut, TOut>>((tc) =>
                {
                    var pipelineResult = tc.IsSuccess
                        ? new PipelineResult<TOut>(tc.Input)
                        : new PipelineResult<TOut>(tc.Exception);

                    tc.TaskCompletionSource.SetResult(pipelineResult);
                }, _options);
            var lastStep = _steps.Last();
            var setResultBlock = ((lastStep as DataflowStep).Block as ISourceBlock<TC<TOut, TOut>>);
            setResultBlock.LinkTo(setResultStep);

            return new Pipeline<TIn, TOut>(_steps);
        }
    }
}