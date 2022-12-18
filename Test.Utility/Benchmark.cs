using System;
using System.Diagnostics;
using Utility;

namespace Test.Utility
{
    static class Benchmark
    {
        public static void Execute()
        {
#if false
            const int loopCount = 1000 * 1000;
            var buffer = new Byte[1024 * 10];
            var sw = new Stopwatch();
            unsafe
            {
                fixed (Byte* ptr = buffer)
                {
                    foreach (var length in new[] { 1, 2, 4, 8, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 28, 29, 30, 21, 32, 33, 34, 35 })
                    {
                        // 常に UInt64 単位の場合
                        ArrayExtensions._THRESHOLD_COPY_MEMORY_BY_LONG_POINTER = 0;
                        sw.Reset();
                        sw.Start();
                        for (var count = loopCount; count > 0; --count)
                            buffer.CopyMemoryTo(8, buffer, 0, length);
                        sw.Stop();
                        var time1 = sw.Elapsed;

                        // 常に Byte 単位の場合
                        ArrayExtensions._THRESHOLD_COPY_MEMORY_BY_LONG_POINTER = int.MaxValue;
                        sw.Reset();
                        sw.Start();
                        for (var count = loopCount; count > 0; --count)
                            buffer.CopyMemoryTo(8, buffer, 0, length);
                        sw.Stop();
                        var time2 = sw.Elapsed;
                        Console.Write($"length={length}: ");
                        if (time1 <= time2)
                            Console.ForegroundColor = ConsoleColor.Cyan;
                        else
                            Console.ResetColor();
                        Console.Write($"UInt64={time1.TotalMilliseconds * 1000:F3}[μsec]");
                        Console.ResetColor();
                        Console.Write(", ");
                        if (time1 >= time2)
                            Console.ForegroundColor = ConsoleColor.Cyan;
                        else
                            Console.ResetColor();
                        Console.Write($"Byte={time2.TotalMilliseconds * 1000:F3}[μsec]");
                        Console.ResetColor();
                        Console.WriteLine();
                    }
                }
            }
#endif
        }
    }
}
