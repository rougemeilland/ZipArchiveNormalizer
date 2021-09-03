using ICSharpCode.SharpZipLib.Zip;
using System;
using System.IO;
using System.Text;

namespace Experiment
{
    class Program
    {
        private const string fileName1 = "\u0061\u030a.txt";
        private const string fileName2 = "\u00e5.txt";

        static void Main(string[] args)
        {
            if (string.Equals(fileName1, fileName2, StringComparison.InvariantCulture))
                Console.WriteLine("The following two strings match in 'InvariantCulture'.");
            else
                Console.WriteLine("The following two strings do not match in 'InvariantCulture'.");

            if (string.Equals(fileName1, fileName2, StringComparison.Ordinal))
                Console.WriteLine("The following two strings match in 'Ordinal'.");
            else
                Console.WriteLine("The following two strings do not match in 'Ordinal'.");
            Console.WriteLine("string1: \"\u0061\u030a.txt\"(\"\\u0061\\u030a.txt\")");
            Console.WriteLine("string2: \"\u00e5.txt\"(\"\\u00e5.txt\")");

            Console.WriteLine("See also 'https://docs.microsoft.com/ja-jp/dotnet/standard/base-types/best-practices-strings#string-operations-that-use-the-invariant-culture'");

            Console.WriteLine();

            var workingDirectoryPath = Path.Combine(args[0], ".MyExperiments");
            try
            {
                ExperimentWithWhetherLocalFileSystemDistinguishesConfusingFilenames(workingDirectoryPath);
                Console.WriteLine();
                ExperimentWithWhetherSharpZipLibDistinguishesConfusingFilenames(workingDirectoryPath);
                Console.WriteLine();
            }
            finally
            {
                Directory.Delete(workingDirectoryPath, false);
            }

            Console.ReadLine();
        }

        private static void ExperimentWithWhetherLocalFileSystemDistinguishesConfusingFilenames(string workingDirectoryPath)
        {
            Console.WriteLine("Attempts to create the following two files on the local file system.");
            Console.WriteLine("path1: \"\u0061\u030a.txt\"(\"\\u0061\\u030a.txt\")");
            Console.WriteLine("path2: \"\u00e5.txt\"(\"\\u00e5.txt\")");

            var path1 = Path.Combine(workingDirectoryPath, fileName1);
            var path2 = Path.Combine(workingDirectoryPath, fileName2);

            Directory.CreateDirectory(workingDirectoryPath);
            try
            {
                File.Delete(path1);
                File.Delete(path2);

                Console.WriteLine("path1: Writing the file...");
                using (var outputStream = new FileStream(path1, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(outputStream, Encoding.UTF8))
                {
                    writer.WriteLine("Hello, world.(1)");
                }
                Console.WriteLine("path1: The file was created successfully.");

                Console.WriteLine("path2: Writing the file...");
                using (var outputStream = new FileStream(path2, FileMode.CreateNew, FileAccess.Write))
                using (var writer = new StreamWriter(outputStream, Encoding.UTF8))
                {
                    writer.WriteLine("Hello, world.(2)");
                }
                Console.WriteLine("path2: The file was created successfully.");

                Console.WriteLine("path1: Reading the file...");
                using (var inputStream = new FileStream(path1, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(inputStream, Encoding.UTF8))
                {
                    Console.WriteLine(
                        string.Format(
                            "path1: {0}",
                            string.Equals(reader.ReadLine(), "Hello, world.(1)", StringComparison.Ordinal)
                                ? "The contents of the file are normal."
                                : "There is an error in the contents of the file."));
                }

                Console.WriteLine("path2: Reading the file...");
                using (var inputStream = new FileStream(path2, FileMode.Open, FileAccess.Read))
                using (var reader = new StreamReader(inputStream, Encoding.UTF8))
                {
                    Console.WriteLine(
                        string.Format(
                            "path2: {0}",
                            string.Equals(reader.ReadLine(), "Hello, world.(2)", StringComparison.Ordinal)
                                ? "The contents of the file are normal."
                                : "There is an error in the contents of the file."));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    string.Format(
                        "Exception occured.: type=\"{0}\", message=\"{1}\", stack trace=\"{2}\"",
                        ex.GetType().FullName,
                        ex.Message,
                        ex.StackTrace));
            }
            finally
            {
                File.Delete(path1);
                File.Delete(path2);
            }
        }

        private static void ExperimentWithWhetherSharpZipLibDistinguishesConfusingFilenames(string workingDirectoryPath)
        {
            Console.WriteLine("Attempts to write the following two files to a ZIP file.");
            Console.WriteLine("path1: \"\u0061\u030a.txt\"(\"\\u0061\\u030a.txt\")");
            Console.WriteLine("path2: \"\u00e5.txt\"(\"\\u00e5.txt\")");

            var path1 = fileName1;
            var path2 = fileName2;
            var zipFilePath = Path.Combine(workingDirectoryPath, "test.zip");
            try
            {
                File.Delete(zipFilePath);
                using (var newZipArchiveFileStream = new FileStream(zipFilePath, FileMode.CreateNew, FileAccess.ReadWrite))
                using (var newZipArchiveOutputStream = new ZipOutputStream(newZipArchiveFileStream))
                {
                    Console.WriteLine("path1: Adding the file to the ZIP file ...");
                    var newEntry1 = new ZipEntry(path1);
                    newEntry1.DateTime = DateTime.Now;
                    newEntry1.CompressionMethod = CompressionMethod.Stored;
                    newEntry1.IsUnicodeText = true;
                    newZipArchiveOutputStream.PutNextEntry(newEntry1);
                    using (var writer = new StreamWriter(newZipArchiveOutputStream, Encoding.UTF8, 8192, true))
                    {
                        writer.WriteLine("Hello, world.(1)");
                    }
                    Console.WriteLine("path1: The file was successfully added to the ZIP file.");

                    Console.WriteLine("path2: Adding the file to the ZIP file ...");
                    var newEntry2 = new ZipEntry(path2);
                    newEntry2.DateTime = DateTime.Now;
                    newEntry2.CompressionMethod = CompressionMethod.Stored;
                    newEntry2.IsUnicodeText = true;
                    newZipArchiveOutputStream.PutNextEntry(newEntry2);
                    using (var writer = new StreamWriter(newZipArchiveOutputStream, Encoding.UTF8, 8192, true))
                    {
                        writer.WriteLine("Hello, world.(2)");
                    }
                    Console.WriteLine("path2: The file was successfully added to the ZIP file.");
                }

                using (var zipFile = new ZipFile(zipFilePath))
                {
                    Console.WriteLine("path1: Reading a file from a ZIP file ...");
                    using (var inputStream = zipFile.GetInputStream(new ZipEntry(path1))) // <= I got an exception here
                    using (var reader = new StreamReader(inputStream))
                    {
                        Console.WriteLine(
                            string.Format(
                                "path1: {0}",
                                string.Equals(reader.ReadLine(), "Hello, world.(1)", StringComparison.Ordinal)
                                    ? "The contents of the file are normal."
                                    : "There is an error in the contents of the file."));
                    }

                    Console.WriteLine("path2: Reading a file from a ZIP file ...");
                    using (var inputStream = zipFile.GetInputStream(new ZipEntry(path2)))
                    using (var reader = new StreamReader(inputStream))
                    {
                        Console.WriteLine(
                            string.Format(
                                "path2: {0}",
                                string.Equals(reader.ReadLine(), "Hello, world.(2)", StringComparison.Ordinal)
                                    ? "The contents of the file are normal."
                                    : "There is an error in the contents of the file."));
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(
                    string.Format(
                        "Exception occured.: type=\"{0}\", message=\"{1}\", stack trace=\"{2}\"",
                        ex.GetType().FullName,
                        ex.Message,
                        ex.StackTrace));
            }
            finally
            {
                File.Delete(zipFilePath);
            }
        }
    }
}