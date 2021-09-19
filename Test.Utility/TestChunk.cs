using System;
using System.Linq;
using Utility;

namespace Test.Utility
{
    static class TestChunk
    {
        public static void Test()
        {
            {
                var r = Enumerable.Range(0, 0).ToChunkOfArray(1).ToArray();
                if (!(r.Length == 0))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 1");
            }
            {
                var r = Enumerable.Range(0, 0).ToChunkOfArray(2).ToArray();
                if (!(r.Length == 0))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 2");
            }
            {
                var r = Enumerable.Range(0, 1).ToChunkOfArray(1).ToArray();
                if (!(r.Length == 1 && r[0].SequenceEqual(new[] { 0 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 3");
            }
            {
                var r = Enumerable.Range(0, 1).ToChunkOfArray(2).ToArray();
                if (!(r.Length == 1 && r[0].SequenceEqual(new[] { 0 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 4");
            }
            {
                var r = Enumerable.Range(0, 2).ToChunkOfArray(1).ToArray();
                if (!(r.Length == 2 && r[0].SequenceEqual(new[] { 0 }) && r[1].SequenceEqual(new[] { 1 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 5");
            }
            {
                var r = Enumerable.Range(0, 2).ToChunkOfArray(2).ToArray();
                if (!(r.Length == 1 && r[0].SequenceEqual(new[] { 0, 1 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 6");
            }
            {
                var r = Enumerable.Range(0, 3).ToChunkOfArray(1).ToArray();
                if (!(r.Length == 3 && r[0].SequenceEqual(new[] { 0 }) && r[1].SequenceEqual(new[] { 1 }) && r[2].SequenceEqual(new[] { 2 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 7");
            }
            {
                var r = Enumerable.Range(0, 3).ToChunkOfArray(2).ToArray();
                if (!(r.Length == 2 && r[0].SequenceEqual(new[] { 0, 1 }) && r[1].SequenceEqual(new[] { 2 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 8");
            }
            {
                var r = Enumerable.Range(0, 4).ToChunkOfArray(2).ToArray();
                if (!(r.Length == 2 && r[0].SequenceEqual(new[] { 0, 1 }) && r[1].SequenceEqual(new[] { 2, 3 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 9");
            }
        }
    }
}
