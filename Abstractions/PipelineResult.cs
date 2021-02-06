using System;
using System.Collections.Generic;

namespace Abstractions
{
    public class PipelineResult<TResult>
    {
        public PipelineResult(TResult value)
        {
            Value = value;
        }

        public PipelineResult(Exception ex)
        {
            Errors = new List<string> {ex.Message};
        }

        public TResult Value { get; }
        public IReadOnlyList<string> Errors { get; }
    }
}