using System;
using System.Linq;
using Utility;

namespace Test.Utility
{
    static class TestBase64
    {
        public static void Test()
        {
            TestBase64Encode();
            TestBase64Decode();
        }

        private static void TestBase64Encode()
        {
            var sourceData = new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47 };

            if (!string.Equals(sourceData.EncodeBase64(Base64EncodingType.Default), "QUJDREVGRw==", StringComparison.Ordinal))
                Console.WriteLine("TestBase64.TestBase64Encode.Default: 処理結果が一致しません。: pattern1");

            if (!string.Equals(sourceData.EncodeBase64(Base64EncodingType.MimeEncoding), "QUJDREVGRw==", StringComparison.Ordinal))
                Console.WriteLine("TestBase64.TestBase64Encode.MimeEncoding: 処理結果が一致しません。: pattern1");

        }

        private static void TestBase64Decode()
        {
            var sourceData = new byte[] { 0x41, 0x42, 0x43, 0x44, 0x45, 0x46, 0x47 };
            var radix64SourceData = new byte[] { 0xc8, 0x38, 0x01, 0x3b, 0x6d, 0x96, 0xc4, 0x11, 0xef, 0xec, 0xef, 0x17, 0xec, 0xef, 0xe3, 0xca, 0x00, 0x04, 0xce, 0x89, 0x79, 0xea, 0x25, 0x0a, 0x89, 0x79, 0x95, 0xf9, 0x79, 0xa9, 0x0a, 0xd9, 0xa9, 0xa9, 0x05, 0x0a, 0x89, 0x0a, 0xc5, 0xa9, 0xc9, 0x45, 0xa9, 0x40, 0xc1, 0xa2, 0xfc, 0xd2, 0xbc, 0x14, 0x85, 0x8c, 0xd4, 0xa2, 0x54, 0x7b, 0x2e, 0x00 };
            var radix64SourceString =
                "  yDgBO22WxBHv7O8X7O/jygAEzol56iUKiXmV+XmpCtmpqQUKiQrFqclFqUDBovzS\r\n" +
                "  vBSFjNSiVHsuAA==\r\n" +
                "  =njUN\r\n";

            if (!"QUJDREVGRw==".DecodeBase64(Base64EncodingType.Default).SequenceEqual(sourceData))
                Console.WriteLine("TestBase64.TestBase64Decode.Default: 処理結果が一致しません。: pattern1");

            if (!"QUJDREVGRw==".DecodeBase64(Base64EncodingType.MimeEncoding).SequenceEqual(sourceData))
                Console.WriteLine("TestBase64.TestBase64Decode.MimeEncoding: 処理結果が一致しません。: pattern1");

            if (!radix64SourceString.DecodeBase64(Base64EncodingType.Radix64Encoding).SequenceEqual(radix64SourceData))
                Console.WriteLine("TestBase64.TestBase64Decode.Radix64Encoding: 処理結果が一致しません。: pattern1");

            if (!sourceData.EncodeBase64(Base64EncodingType.Radix64Encoding).DecodeBase64(Base64EncodingType.Radix64Encoding).SequenceEqual(sourceData))
                Console.WriteLine("TestBase64.TestBase64Decode.Radix64Encoding: 処理結果が一致しません。: pattern2");

            if (!radix64SourceData.EncodeBase64(Base64EncodingType.Radix64Encoding).DecodeBase64(Base64EncodingType.Radix64Encoding).SequenceEqual(radix64SourceData))
                Console.WriteLine("TestBase64.TestBase64Decode.Radix64Encoding: 処理結果が一致しません。: pattern3");
        }
    }
}
