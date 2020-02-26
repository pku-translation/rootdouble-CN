using System;
using System.Linq;
using BenchmarkDotNet.Attributes;

namespace CsYetiTools
{   
    
    [InnerIterationCount(10000)]
    public class Benchmarks
    {
        public static void Run()
        {
            BenchmarkDotNet.Running.BenchmarkRunner.Run<Benchmarks>();
        }
    }
}