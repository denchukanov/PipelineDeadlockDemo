using System;
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
                    // throw new InvalidOperationException($"not implemented ololo {str}");

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


            var test = await pipeline.Execute("PIPE-2");
            var test3 = await pipeline.Execute("PIPE-3");

            Console.WriteLine("End!");
            Console.ReadKey();
        }
    }
}