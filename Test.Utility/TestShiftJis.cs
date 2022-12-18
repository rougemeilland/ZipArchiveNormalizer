using System;
using System.IO;
using Utility;
using Utility.IO;
using Utility.Text;

namespace Test.Utility
{
    static class TestShiftJis
    {
        public static void Test()
        {
            TestAozoraBunkoImageTagReplacement(
                new FileInfo(@"D:\テストデータ\SHIFT-JISのドキュメント1.txt"),
                new FileInfo(@"D:\テストデータ\更新されたSHIFT-JISのドキュメント1.txt"));
#if true
            TestAozoraBunkoImageTagReplacement(
                new FileInfo(@"D:\テストデータ\SHIFT-JISのドキュメント2.txt"),
                new FileInfo(@"D:\テストデータ\更新されたSHIFT-JISのドキュメント2.txt"));
            TestAozoraBunkoImageTagReplacement(
                new FileInfo(@"D:\テストデータ\SHIFT-JISのドキュメント3.txt"),
                new FileInfo(@"D:\テストデータ\更新されたSHIFT-JISのドキュメント3.txt"));
#endif
        }

        private static void TestAozoraBunkoImageTagReplacement(FileInfo sourceFile, FileInfo destinationFile)
        {
            destinationFile.Delete();
            destinationFile.WriteAllBytes(
                sourceFile.OpenRead()
                .GetByteSequence()
                .DecodeAsShiftJisChar()
                .ReplaceAozoraBunkoImageTag(path => path)
                .EncodeAsShiftJisChar());

            var result = sourceFile.OpenRead().StreamBytesEqual(destinationFile.OpenRead());
            Console.WriteLine(String.Format("{0}: {1}", sourceFile.FullName, result ? "OK" : "NG"));
        }
    }
}
