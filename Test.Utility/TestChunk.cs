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
                var r = Enumerable.Range(0, 0).ChunkAsArray(1).ToArray();
                if (!(r.Length == 0))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 1");
            }
            {
                var r = Enumerable.Range(0, 0).ChunkAsArray(2).ToArray();
                if (!(r.Length == 0))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 2");
            }
            {
                var r = Enumerable.Range(0, 1).ChunkAsArray(1).ToArray();
                if (!(r.Length == 1 && r[0].SequenceEqual(new[] { 0 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 3");
            }
            {
                var r = Enumerable.Range(0, 1).ChunkAsArray(2).ToArray();
                if (!(r.Length == 1 && r[0].SequenceEqual(new[] { 0 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 4");
            }
            {
                var r = Enumerable.Range(0, 2).ChunkAsArray(1).ToArray();
                if (!(r.Length == 2 && r[0].SequenceEqual(new[] { 0 }) && r[1].SequenceEqual(new[] { 1 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 5");
            }
            {
                var r = Enumerable.Range(0, 2).ChunkAsArray(2).ToArray();
                if (!(r.Length == 1 && r[0].SequenceEqual(new[] { 0, 1 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 6");
            }
            {
                var r = Enumerable.Range(0, 3).ChunkAsArray(1).ToArray();
                if (!(r.Length == 3 && r[0].SequenceEqual(new[] { 0 }) && r[1].SequenceEqual(new[] { 1 }) && r[2].SequenceEqual(new[] { 2 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 7");
            }
            {
                var r = Enumerable.Range(0, 3).ChunkAsArray(2).ToArray();
                if (!(r.Length == 2 && r[0].SequenceEqual(new[] { 0, 1 }) && r[1].SequenceEqual(new[] { 2 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 8");
            }
            {
                var r = Enumerable.Range(0, 4).ChunkAsArray(2).ToArray();
                if (!(r.Length == 2 && r[0].SequenceEqual(new[] { 0, 1 }) && r[1].SequenceEqual(new[] { 2, 3 })))
                    Console.WriteLine("TestChunk: 処理結果が一致しません。: pattern = 9");
            }
        }
    }
}
