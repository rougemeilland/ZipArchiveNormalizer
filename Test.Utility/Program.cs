using System;
using Utility;

namespace Test.Utility
{
    class Program
    {
        static void Main(string[] args)
        {
#if false
            foreach (var lineText in CrcTableMaker.GetCrc32Table())
                Console.WriteLine(lineText);
            foreach (var lineText in CrcTableMaker.GetRadix64Table())
                Console.WriteLine(lineText);
#endif
            TestFilePathNameComparer.Test();
            TestChunk.Test();
            TestTinyBitArray.Test();
            TestBitQueue.Test();
            TestBase64.Test();
            Console.WriteLine();
            Console.WriteLine("完了しました。");
            Console.Beep();
            Console.ReadLine();
        }
    }
}
