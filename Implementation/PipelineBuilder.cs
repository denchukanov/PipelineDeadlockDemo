using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
using Abstractions;

namespace Implementation
{
    internal class PipelineBuilder<TIn, TOut> : IPipelineBuilder<TIn, TOut>
    {
        private readonly List<IStep> _steps = new();
        private readonly ExecutionDataflowBlockOptions _options;

        public PipelineBuilder()
        {
            _options = new ExecutionDataflowBlockOptions()
            {
                MaxDegreeOfParallelism = DataflowBlockOptions.Unbounded,
                // CancellationToken = cts?.Token ?? CancellationToken.None
            };
        }


        public IPipelineBuilder<TIn, TOut> AddStep<TStepIn, TStepOut>(Func<TStepIn, TStepOut> stepFn)
        {
            if (_steps.Count == 0)
            {
                var step = SyncInSyncOut(stepFn);
                _steps.Add(DataflowStep.Sync(step));
            }
            else
            {
                var lastStep = _steps.Last();

                if (lastStep.IsAsync)
                {
                    var step = AsyncInSyncOut(stepFn);
                    var targetBlock = ((lastStep as DataflowStep).Block as ISourceBlock<TCAsync<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Sync(step));
                }
                else
                {
                    var step = SyncInSyncOut(stepFn);
                    var targetBlock = ((lastStep as DataflowStep).Block as ISourceBlock<TC<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Sync(step));
                }
            }

            return this;
        }

        private TransformBlock<TCAsync<TStepIn, TOut>, TC<TStepOut, TOut>> AsyncInSyncOut<TStepIn, TStepOut>(
            Func<TStepIn, TStepOut> stepFn)
        {
            return new TransformBlock<TCAsync<TStepIn, TOut>, TC<TStepOut, TOut>>(async (tc) =>
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
        }

     
        private TransformBlock<TC<TStepIn, TOut>, TC<TStepOut, TOut>> SyncInSyncOut<TStepIn, TStepOut>(
            Func<TStepIn, TStepOut> fn)
        {
            return new TransformBlock<TC<TStepIn, TOut>, TC<TStepOut, TOut>>((tc) =>
            {
                TC<TStepOut, TOut> result;
                try
                {
                    if (!tc.IsSuccess) return new TC<TStepOut, TOut>(tc.TaskCompletionSource, tc.Exception);

                    var stepResult = fn(tc.Input);
                    result = new TC<TStepOut, TOut>(stepResult, tc.TaskCompletionSource);
                }
                catch (Exception e)
                {
                    result = new TC<TStepOut, TOut>(tc.TaskCompletionSource, e);
                }

                return result;
            }, _options);
        }


        public IPipelineBuilder<TIn, TOut> AddStepAsync<TStepIn, TStepOut>(Func<TStepIn, Task<TStepOut>> stepFn)
        {
            if (_steps.Count == 0)
            {
                var step = SyncInAsyncOut(stepFn);
                _steps.Add(DataflowStep.Async(step));
            }
            else
            {
                var lastStep = _steps.Last();

                if (lastStep.IsAsync)
                {
                    var step = AsyncInAsyncOut(stepFn);
                    var targetBlock = ((lastStep as DataflowStep).Block as ISourceBlock<TCAsync<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Async(step));
                }
                else
                {
                    var step = SyncInAsyncOut(stepFn);
                    var targetBlock = ((lastStep as DataflowStep).Block as ISourceBlock<TC<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Async(step));
                }
            }

            return this;
        }

        private TransformBlock<TCAsync<TStepIn, TOut>, TCAsync<TStepOut, TOut>> AsyncInAsyncOut<TStepIn, TStepOut>(
            Func<TStepIn, Task<TStepOut>> stepFn)
        {
            return new TransformBlock<TCAsync<TStepIn, TOut>, TCAsync<TStepOut, TOut>>(async (tc) =>
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
        }

        private TransformBlock<TC<TStepIn, TOut>, TCAsync<TStepOut, TOut>> SyncInAsyncOut<TStepIn, TStepOut>(
            Func<TStepIn, Task<TStepOut>> stepFn)
        {
            return new TransformBlock<TC<TStepIn, TOut>, TCAsync<TStepOut, TOut>>((tc) =>
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
        }

        public IPipeline<TIn, TOut> Create()
        {
            if (_steps.Count == 0) throw new InvalidOperationException();


            var lastStep = _steps.Last();

            if (lastStep.IsAsync)
            {
                var setResultStep =
                    new ActionBlock<TCAsync<TOut, TOut>>(async (tc) =>
                    {
                        PipelineResult<TOut> pipelineResult;
                        if (tc.IsSuccess)
                        {
                            try
                            {
                                var result = await tc.Input;
                                pipelineResult = new PipelineResult<TOut>(result);
                            }
                            catch (Exception e)
                            {
                                pipelineResult = new PipelineResult<TOut>(e);
                            }
                        }
                        else
                        {
                            pipelineResult = new PipelineResult<TOut>(tc.Exception);
                        }

                        tc.TaskCompletionSource.SetResult(pipelineResult);
                    }, _options);
                var setResultBlock = ((lastStep as DataflowStep).Block as ISourceBlock<TCAsync<TOut, TOut>>);
                setResultBlock.LinkTo(setResultStep);
            }
            else
            {
                var setResultStep =
                    new ActionBlock<TC<TOut, TOut>>((tc) =>
                    {
                        var pipelineResult = tc.IsSuccess
                            ? new PipelineResult<TOut>(tc.Input)
                            : new PipelineResult<TOut>(tc.Exception);

                        tc.TaskCompletionSource.SetResult(pipelineResult);
                    }, _options);
                var lastStepAsSource = ((lastStep as DataflowStep).Block as ISourceBlock<TC<TOut, TOut>>);
                lastStepAsSource.LinkTo(setResultStep);
            }

            return new Pipeline<TIn, TOut>(_steps);
        }
    }
}
