using System;
using System.Linq;
using System.Threading.Tasks;
using Abstractions;
using Implementation;
using Xunit;

namespace Tests
{
    public class PipelineTests
    {
        // var pipeline = new PipelineBuilderFactory()
        //     .NewBuilder<string, string>()
        //     .AddStep<string, (string, int)>(str =>
        //     {
        //         var value = int.Parse(str.Split('-')[1]);
        //         return (str, value);
        //     })
        //     .AddStepAsync<(string Str, int Int), string>(async input =>
        //     {
        //         if (input.Int % 2 == 0)
        //         {
        //             throw new InvalidOperationException(input.Int.ToString());
        //         }
        //         await Task.Delay(500);
        //         return input.Str;
        //     })
        //     .AddStep<string, string>(str => str)
        //     .Create();
        [Fact]
        public async Task SimpleTest()
        {
            var pipeline = new PipelineBuilderFactory()
                .NewBuilder<string, string>()
                .AddStep<string, (string, int)>(str =>
                {
                    var value = int.Parse(str.Split('-')[1]);
                    return (str, value);
                })
                .AddStepAsync<(string Str, int Int), string>(async input =>
                {
                    if (input.Int % 2 == 0)
                    {
                        Console.WriteLine($"{input.Str} ERROR");
                        throw new InvalidOperationException(input.Int.ToString());
                    }

                    Console.WriteLine($"Start {input.Str} evaluation");
                    await Task.Delay(500);
                    Console.WriteLine($"End {input.Str} evaluation");
                    return input.Str;
                })
                .AddStep<string, string>(str => throw new InvalidOperationException("str"))
                .Create();

            var tasks = Enumerable.Range(1, 6).AsParallel().Select(async x => await pipeline.Execute($"PIPE-{x}"));
            var results = await Task.WhenAll(tasks);

            Assert.Equal(tasks.Count(), results.Count());
        }

        [Fact]
        public async Task FirstSyncStepOfTwoThrowException()
        {
            var pipeline = new PipelineBuilderFactory()
                .NewBuilder<string, string>()
                .AddStep<string, (string, int)>(str => throw new InvalidOperationException(str))
                .AddStep<(string Str, int Int), string>(input => input.Str)
                .Create();

            var tasks = Enumerable.Range(1, 3)
                .AsParallel()
                .Select(async x => await pipeline.Execute($"PIPE-{x}"))
                .ToList();
            var results = await Task.WhenAll(tasks);

            Assert.Equal(tasks.Count, results.Count());
        }

        [Fact]
        public async Task SecondSyncStepOfTwoThrowException()
        {
            var pipeline = new PipelineBuilderFactory()
                .NewBuilder<string, string>()
                .AddStep<string, (string, int)>(str =>
                {
                    var value = int.Parse(str.Split('-')[1]);
                    return (str, value);
                })
                .AddStep<(string Str, int Int), string>(input => throw new InvalidOperationException(input.Str))
                .Create();

            var tasks = Enumerable.Range(1, 3)
                .AsParallel()
                .Select(async x => await pipeline.Execute($"PIPE-{x}"))
                .ToList();
            var results = await Task.WhenAll(tasks);

            Assert.Equal(tasks.Count, results.Count());
        }

        [Fact]
        public async Task FirstAsyncStepOfTwoThrowException()
        {
            var pipeline = new PipelineBuilderFactory()
                .NewBuilder<string, string>()
                .AddStepAsync<string, (string, int)>(str => throw new InvalidOperationException(str))
                .AddStepAsync<(string Str, int Int), string>(input=> Task.FromResult(input.Str))
                .Create();

            var tasks = Enumerable.Range(1, 3)
                .AsParallel()
                .Select(async x => await pipeline.Execute($"PIPE-{x}"))
                .ToList();
            var results = await Task.WhenAll(tasks);

            Assert.Equal(tasks.Count, results.Count());
        }

        [Fact]
        public async Task SecondAsyncStepOfTwoThrowException()
        {
            var pipeline = new PipelineBuilderFactory()
                .NewBuilder<string, string>()
                .AddStepAsync<string, (string, int)>(str =>
                {
                    var value = int.Parse(str.Split('-')[1]);
                    return Task.FromResult((str, value));
                })
                .AddStepAsync<(string Str, int Int), string>(input => throw new InvalidOperationException(input.Str))
                .Create();

            var tasks = Enumerable.Range(1, 3)
                .AsParallel()
                .Select(async x => await pipeline.Execute($"PIPE-{x}"))
                .ToList();
            var results = await Task.WhenAll(tasks);

            Assert.Equal(tasks.Count, results.Count());
        }

        public async Task FirstSyncStepOfThreeThrowException()
        {
        }
        
        public async Task SecondSyncStepOfThreeThrowException()
        {
        }
        
        public async Task ThirdSyncStepOfThreeThrowException()
        {
        }
        
        public async Task FirstAsyncStepOfThreeThrowException()
        {
        }
        
        public async Task SecondAsyncStepOfThreeThrowException()
        {
        }
        
        public async Task ThirdAsyncStepOfThreeThrowException()
        {
        }
    }
}