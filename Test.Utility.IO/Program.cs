using System;

namespace Test.Utility.IO
{
    class Program
    {
        static void Main(string[] args)
        {
            //TestFifoBuffer.Test();
            TestBufferedInputStream.Test();
            TestBufferedOutputStream.Test();
            TestBitInputStream.Test();
            TestBitOutputStream.Test();
            Console.WriteLine();
            Console.WriteLine("OK");
            Console.Beep();
            Console.ReadLine();
        }
    }
}
