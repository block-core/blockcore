using BenchmarkDotNet.Running;

namespace Blockcore.Benchmark
{
    internal class Program
    {
        private static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);

            // for debug benchmark, adds "new DebugInProcessConfig()"
            //BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
        }
    }
}