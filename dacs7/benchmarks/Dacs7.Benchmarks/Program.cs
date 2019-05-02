using BenchmarkDotNet.Running;

namespace Dacs7.Benchmarks
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            //var summary = BenchmarkRunner.Run<TypeOfBenchmarks>();
            var summary = BenchmarkRunner.Run<ReadBenchmark>();
        }
    }
}
