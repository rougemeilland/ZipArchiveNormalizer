using System;
using Utility;

namespace ZipArchiveNormalizer
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var canceller = new ConsoleBreakCancellale())
            using (var worker = new NormalizerWorker(canceller))
            {
                worker.Execute(args);
            }
            Console.WriteLine("Enterを押してください。");
            Console.Beep();
            Console.ReadLine();
        }
    }
}