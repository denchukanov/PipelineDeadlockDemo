using System;
using System.Threading.Tasks;
using Abstractions;

namespace Implementation
{
    internal class TCAsync<TInput, TOutput>
    {
        public TCAsync(Task<TInput> input, TaskCompletionSource<PipelineResult<TOutput>> tcs)
        {
            Input = input;
            TaskCompletionSource = tcs;
            Exception = null;
            IsSuccess = true;
        }


        public TCAsync(TaskCompletionSource<PipelineResult<TOutput>> tcs, Exception exception)
        {
            Input = Task.FromResult(default(TInput));
            TaskCompletionSource = tcs;
            Exception = exception;
            IsSuccess = false;
        }

        public TCAsync(Task<TInput> input, TaskCompletionSource<PipelineResult<TOutput>> tcs, Exception exception)
        {
            Input = input;
            TaskCompletionSource = tcs;
            Exception = exception;
            IsSuccess = false;
        }

        public Task<TInput> Input { get; }
        public TaskCompletionSource<PipelineResult<TOutput>> TaskCompletionSource { get; }
        public bool IsSuccess { get; }
        public Exception Exception { get; }
    }
}