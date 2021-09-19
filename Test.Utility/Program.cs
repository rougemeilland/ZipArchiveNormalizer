using System;

namespace Test.Utility
{
    class Program
    {
        static void Main(string[] args)
        {
            TestFilePathNameComparer.Test();
            TestChunk.Test();
            TestReadOnlySerializedBitArray.Test();
            TestSerializedBitArray.Test();



            Console.WriteLine("完了しました。                                ");
            Console.Beep();
            Console.ReadLine();
        }
    }
}
