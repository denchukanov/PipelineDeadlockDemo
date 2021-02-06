using System;

namespace Abstractions
{
    public class PipelineResult<TResult>
    {
        public PipelineResult(TResult value)
        {
            Value = value;
            Status = PipelineResultStatus.Success;
        }

        public PipelineResult(Exception ex)
        {
            Exception = ex;
            Status = PipelineResultStatus.Exception;
        }

        public TResult Value { get; }
        public PipelineResultStatus Status { get; }

        public Exception Exception { get; }
    }
}