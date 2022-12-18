using System;

namespace Test.Utility
{
    class Program
    {
        static void Main(string[] args)
        {
#if false
            // CRCテーブルの生成
            foreach (var lineText in CrcTableMaker.GetCrc32Table())
                Console.WriteLine(lineText);
            foreach (var lineText in CrcTableMaker.GetRadix64Table())
                Console.WriteLine(lineText);
#endif
#if false
            TestFilePathNameComparer.Test();
            TestChunk.Test();
            TestTinyBitArray.Test();
            TestBitQueue.Test();
            TestBase64.Test();
            TestCopyMemory.Test();
#endif
            TestShiftJis.Test();
            Console.WriteLine();
            Console.WriteLine("完了しました。");
            Console.Beep();
            Console.ReadLine();
        }
    }
}
