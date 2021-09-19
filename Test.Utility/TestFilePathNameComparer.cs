using System;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Utility;

namespace Test.Utility
{
    static class TestFilePathNameComparer
    {
        private static Regex _digitsPattern = new Regex(@"(?<digits>[0-9]+)", RegexOptions.Compiled);

        public static void Test()
        {
            var basicStrings = new[]
            {
                "",
                "a",
                "abc",
                "cde",
                "A",
                "ABC",
                "CDE",
                "0",
                "1",
                "01",
                "10",
                "001",
                "010",
                "100",
                "/",
                @"\",
                "mimetype",
                "META-INF",
            };
            var random = new Random();
            var testStringPatterns =
                basicStrings
                .SelectMany(s => basicStrings, (s1, s2) => s1 + s2)
                .SelectMany(s => basicStrings, (s1, s2) => s1 + s2)
                .Distinct()
                .ToReadOnlyCollection();
            var basicOptions = new[]
            {
                FilePathNameComparerrOption.None,
                FilePathNameComparerrOption.IgnoreCase,
                FilePathNameComparerrOption.ConsiderContentFile,
                FilePathNameComparerrOption.ConsiderDigitSequenceOfsAsNumber,
                FilePathNameComparerrOption.ConsiderPathNameDelimiter,
            };
            var testOptionPatterns =
                basicOptions
                .SelectMany(value => basicOptions, (value1, value2) => value1 | value2)
                .SelectMany(value => basicOptions, (value1, value2) => value1 | value2)
                .SelectMany(value => basicOptions, (value1, value2) => value1 | value2)
                .SelectMany(value => basicOptions, (value1, value2) => value1 | value2)
                .Distinct()
                .OrderBy(value => value)
                .Where(value => value != FilePathNameComparerrOption.None)
                .Concat(new[] { FilePathNameComparerrOption.None })
                .ToReadOnlyCollection();
            var testCulturePatterns = new[]
            {
                null,
                CultureInfo.InvariantCulture,
                CultureInfo.CurrentCulture,
            };

            var lockObject = new object();

            var totalCount = (long)testOptionPatterns.Count * testCulturePatterns.Length * testStringPatterns.Count * testStringPatterns.Count;
            var count = 0L;
            var previousText = "";
            var startTime = DateTime.Now;
            testOptionPatterns
                .SelectMany(option => testCulturePatterns, (option, culture) => new { option, culture })
                .Select(item => new { comparer = new FilePathNameComparer(item.option, item.culture), item.option, item.culture })
                .SelectMany(item => testStringPatterns, (item, s1) => new { item.comparer, item.option, item.culture, s1 })
                .SelectMany(item => testStringPatterns, (item, s2) => new { item.comparer, item.option, item.culture, item.s1, s2 })
                .AsParallel()
                .WithDegreeOfParallelism(Environment.ProcessorCount)
                .ForAll(item =>
                {
                    try
                    {
                        var actualResult = item.comparer.Compare(item.s1, item.s2);
                        var desiredResult = GetDesiredResult(item.s1, item.s2, item.option, item.culture);
                        if (!actualResult.SignEquals(desiredResult))
                        {
                            lock (lockObject)
                            {
                                Console.WriteLine(
                                    string.Format(
                                        "処理結果が一致しません。: actual={0}, desired={1}, s1=\"{2}\", s2=\"{3}\", option=\"{4}\", culture=\"{5}\"",
                                        actualResult,
                                        desiredResult,
                                        item.s1,
                                        item.s2,
                                        item.option,
                                        item.culture?.Name ?? "(null)"));
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                        lock (lockObject)
                        {
                            Console.WriteLine(string.Format("例外が発生しました。: message=\"{0}\", stack trace=\"{1}\"", ex.Message, ex.StackTrace));
                        }
                    }
                    finally
                    {
                        lock (lockObject)
                        {
                            ++count;
                            var now = DateTime.Now;
                            var text =
                                string.Format(
                                    "{0}%完了 (終了予定: {1:yyyy年MM月dd日 HH時mm分})",
                                    count * 100 / totalCount,
                                    now.AddSeconds((now - startTime).TotalSeconds * (totalCount - count) / count));
                            if (previousText != text)
                            {
                                Console.Write(text + "          \r");
                                previousText = text;
                            }


                            // count : totalCount - count == now - startTime : x - now
                            // (now - startTime) * (totalCount - count) == (x - now) * count
                            // x - now = (now - startTime) * (totalCount - count) / count
                            // x =  now + (now - startTime) * (totalCount - count) / count
                        }
                    }
                });
        }

        private static int GetDesiredResult(string s1, string s2, FilePathNameComparerrOption option, CultureInfo culture)
        {
            if (option.HasFlag(FilePathNameComparerrOption.ConsiderContentFile))
            {
                if (s1 == "mimetype")
                {
                    if (s2 == "mimetype")
                    {
                        // CONTINUE
                    }
                    else
                        return -1;
                }
                else if (
                    s1 == "META-INF" ||
                    s1.StartsWith(@"META-INF/", StringComparison.Ordinal) ||
                    s1.StartsWith(@"META-INF\", StringComparison.Ordinal))
                {
                    if (s2 == "mimetype")
                        return 1;
                    else if (s2 == "META-INF" || s2.StartsWith(@"META-INF/", StringComparison.Ordinal) || s2.StartsWith(@"META-INF\", StringComparison.Ordinal))
                    {
                        // CONTINUE
                    }
                    else
                        return -1;
                }
                else
                {
                    if (s2 == "mimetype" ||
                        s2 == "META-INF" ||
                        s2.StartsWith(@"META-INF/", StringComparison.Ordinal) ||
                        s2.StartsWith(@"META-INF\", StringComparison.Ordinal))
                    {
                        return 1;
                    }
                    else
                    {
                        // CONTINUE
                    }
                }
            }
            if (option.HasFlag(FilePathNameComparerrOption.ConsiderDigitSequenceOfsAsNumber))
            {
                s1 = _digitsPattern.Replace(s1, m => m.Groups["digits"].Value.PadLeft(10, '0'));
                s2 = _digitsPattern.Replace(s2, m => m.Groups["digits"].Value.PadLeft(10, '0'));
            }
            var ignoreCase = option.HasFlag(FilePathNameComparerrOption.IgnoreCase);
            if (option.HasFlag(FilePathNameComparerrOption.ConsiderPathNameDelimiter))
            {
                var sequence1 = s1.Split('/', '\\');
                var sequence2 = s2.Split('/', '\\');
                return
                    sequence1
                    .SequenceCompare(
                        sequence2,
                        new CustomizableComparer<string>(
                            (x, y) =>
                            {
                                if (culture == null)
                                    return string.Compare(x, y, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                                else
                                    return string.Compare(x, y, ignoreCase, culture);
                            }));
            }
            else
            {
                if (culture == null)
                    return string.Compare(s1, s2, ignoreCase ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal);
                else
                    return string.Compare(s1, s2, ignoreCase, culture);
            }
        }
    }
}
