using System;
using System.IO;

namespace Test.ZipUtility
{
    class Program
    {
        static void Main(string[] args)
        {
            TestDataFile.Create(new DirectoryInfo(@"D:\テストデータ"));
            //ValidationOfZipFile.Test(args);
            Console.WriteLine();
            Console.WriteLine("OK");
            Console.Beep();
            Console.ReadLine();
        }
    }
}