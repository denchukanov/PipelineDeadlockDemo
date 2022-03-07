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
                    var targetBlock =
                        ((lastStep as DataflowStep).Block as ISourceBlock<AsyncStepContext<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Sync(step));
                }
                else
                {
                    var step = SyncInSyncOut(stepFn);
                    var targetBlock =
                        ((lastStep as DataflowStep).Block as ISourceBlock<SyncStepContext<TStepIn, TOut>>);
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
                var step = SyncInAsyncOut(stepFn);
                _steps.Add(DataflowStep.Async(step));
            }
            else
            {
                var lastStep = _steps.Last();

                if (lastStep.IsAsync)
                {
                    var step = AsyncInAsyncOut(stepFn);
                    var targetBlock =
                        ((lastStep as DataflowStep).Block as ISourceBlock<AsyncStepContext<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Async(step));
                }
                else
                {
                    var step = SyncInAsyncOut(stepFn);
                    var targetBlock =
                        ((lastStep as DataflowStep).Block as ISourceBlock<SyncStepContext<TStepIn, TOut>>);
                    targetBlock.LinkTo(step, new DataflowLinkOptions());
                    _steps.Add(DataflowStep.Async(step));
                }
            }

            return this;
        }

        public IPipeline<TIn, TOut> Create()
        {
            if (_steps.Count == 0) throw new InvalidOperationException();


            var lastStep = _steps.Last();

            if (lastStep.IsAsync)
            {
                var setResultStep =
                    new ActionBlock<AsyncStepContext<TOut, TOut>>(async (tc) =>
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
                
                var setResultBlock = ((lastStep as DataflowStep).Block as ISourceBlock<AsyncStepContext<TOut, TOut>>);
                setResultBlock.LinkTo(setResultStep);
            }
            else
            {
                var setResultStep =
                    new ActionBlock<SyncStepContext<TOut, TOut>>((tc) =>
                    {
                        var pipelineResult = tc.IsSuccess
                                                 ? new PipelineResult<TOut>(tc.Input)
                                                 : new PipelineResult<TOut>(tc.Exception);

                        tc.TaskCompletionSource.SetResult(pipelineResult);
                    }, _options);
                
                var lastStepAsSource = ((lastStep as DataflowStep).Block as ISourceBlock<SyncStepContext<TOut, TOut>>);
                lastStepAsSource.LinkTo(setResultStep);
            }

            return new Pipeline<TIn, TOut>(_steps);
        }
        
        private TransformBlock<AsyncStepContext<TStepIn, TOut>, SyncStepContext<TStepOut, TOut>>
            AsyncInSyncOut<TStepIn, TStepOut>(Func<TStepIn, TStepOut> stepFn)
        {
            Func<AsyncStepContext<TStepIn, TOut>, Task<SyncStepContext<TStepOut, TOut>>> transform = async tc =>
            {
                try
                {
                    return !tc.IsSuccess
                               ? new SyncStepContext<TStepOut, TOut>(tc.TaskCompletionSource, tc.Exception)
                               : new SyncStepContext<TStepOut, TOut>(stepFn(await tc.Input), tc.TaskCompletionSource);
                }
                catch (Exception e)
                {
                    return new SyncStepContext<TStepOut, TOut>(tc.TaskCompletionSource, e);
                }
            };

            return new TransformBlock<AsyncStepContext<TStepIn, TOut>, SyncStepContext<TStepOut, TOut>>(
                transform, _options);
        }

        private TransformBlock<SyncStepContext<TStepIn, TOut>, SyncStepContext<TStepOut, TOut>>
            SyncInSyncOut<TStepIn, TStepOut>(Func<TStepIn, TStepOut> fn)
        {
            Func<SyncStepContext<TStepIn, TOut>, SyncStepContext<TStepOut, TOut>> transform = tc =>
            {
                try
                {
                    return !tc.IsSuccess
                               ? new SyncStepContext<TStepOut, TOut>(tc.TaskCompletionSource, tc.Exception)
                               : new SyncStepContext<TStepOut, TOut>(fn(tc.Input), tc.TaskCompletionSource);
                }
                catch (Exception e)
                {
                    return new SyncStepContext<TStepOut, TOut>(tc.TaskCompletionSource, e);
                }
            };


            return new TransformBlock<SyncStepContext<TStepIn, TOut>, SyncStepContext<TStepOut, TOut>>(
                transform, _options);
        }

        private TransformBlock<AsyncStepContext<TStepIn, TOut>, AsyncStepContext<TStepOut, TOut>>
            AsyncInAsyncOut<TStepIn, TStepOut>(Func<TStepIn, Task<TStepOut>> stepFn)
        {
            Func<AsyncStepContext<TStepIn, TOut>, Task<AsyncStepContext<TStepOut, TOut>>> transform = async tc =>
            {
                try
                {
                    return !tc.IsSuccess
                               ? new AsyncStepContext<TStepOut, TOut>(tc.TaskCompletionSource, tc.Exception)
                               : new AsyncStepContext<TStepOut, TOut>(stepFn(await tc.Input), tc.TaskCompletionSource);
                }
                catch (Exception e)
                {
                    return new AsyncStepContext<TStepOut, TOut>(tc.TaskCompletionSource, e);
                }
            };

            return new TransformBlock<AsyncStepContext<TStepIn, TOut>, AsyncStepContext<TStepOut, TOut>>(
                transform, _options);
        }


        private TransformBlock<SyncStepContext<TStepIn, TOut>, AsyncStepContext<TStepOut, TOut>>
            SyncInAsyncOut<TStepIn, TStepOut>(Func<TStepIn, Task<TStepOut>> stepFn)
        {
            Func<SyncStepContext<TStepIn, TOut>, AsyncStepContext<TStepOut, TOut>> transform = (tc) =>
            {
                try
                {
                    return !tc.IsSuccess
                               ? new AsyncStepContext<TStepOut, TOut>(tc.TaskCompletionSource, tc.Exception)
                               : new AsyncStepContext<TStepOut, TOut>(stepFn(tc.Input), tc.TaskCompletionSource);
                }
                catch (Exception e)
                {
                    return new AsyncStepContext<TStepOut, TOut>(tc.TaskCompletionSource, e);
                }
            };

            return new TransformBlock<SyncStepContext<TStepIn, TOut>, AsyncStepContext<TStepOut, TOut>>(
                transform, _options);
        }

        
    }
}
