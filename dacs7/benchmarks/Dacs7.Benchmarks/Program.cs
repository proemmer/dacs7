using BenchmarkDotNet.Running;
using System;

namespace Dacs7.Benchmarks
{
    class Program
    {
        static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<TypeOfBenchmarks>();
        }
    }
}
