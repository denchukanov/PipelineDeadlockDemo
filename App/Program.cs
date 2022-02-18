using System;
using System.Linq;
using System.Threading.Tasks;
using Implementation;

namespace App
{
    class StringInt
    {
        public string Str { get; }
        public int Int { get; }

        public StringInt(string str, int i)
        {
            Str = str;
            Int = i;
        }
    }

    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var pipeline = new PipelineBuilderFactory()
                .NewBuilder<string, string>()
                .AddStep<string, StringInt>(str =>
                {
                    Console.WriteLine($"Start {str}");

                    // TODO NOTE: in that case we will be deadlocked
                    // throw new InvalidOperationException($"not implemented {str}");

                    var value = int.Parse(str.Split('-')[1]);
                    return new StringInt(str, value);
                })
                .AddStepAsync<StringInt, string>(async str =>
                {
                    if (str.Int % 2 == 0)
                    {
                        Console.WriteLine($"{str.Str} ERROR");
                        throw new InvalidOperationException(str.Int.ToString());
                    }

                    Console.WriteLine($"Start {str.Str} evaluation");
                    await Task.Delay(500);
                    Console.WriteLine($"End {str.Str} evaluation");
                    return str.Str;
                })
                .AddStep<string, string>(str =>
                {
                    Console.WriteLine($"End of {str} pipeline");
                    return str;
                })
                .Create();

            var result = await pipeline.Execute("test-1");

            // var tasks = Enumerable.Range(1, 100).AsParallel().Select(async x => await pipeline.Execute($"PIPE-{x}"));
            // var results = await Task.WhenAll(tasks);
            // foreach (var res in results)
            // {
                // Console.WriteLine(res?.Value ?? res?.Exception?.Message);
            // }

            Console.WriteLine("End!");
            Console.ReadKey();
        }
    }
}
