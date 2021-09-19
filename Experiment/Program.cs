using System;
using System.Linq;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Utility.IO;
using Utility;
using ZipUtility;
using ZipUtility.Compression;

namespace Experiment
{
    class Program
    {
        static void Main(string[] args)
        {
            var factor1 = 2.0D;
            var factor2 = 96.0D;

            for (var n = 1; n < 20; ++n)
            {
                var x = Math.Log(factor2) * n / Math.Log(factor1);
                var y = Math.Ceiling(x);
                var percent = x * 100 / y;
                if (y < 64.0)
                    Console.WriteLine(string.Format("{0:d2}: x={1:F2}, bits={2:F0}, percentage={3:F0}", n, x, y, percent));
            }
            Console.WriteLine("OK");
            Console.Beep();
            Console.ReadLine();
        }
    }
}