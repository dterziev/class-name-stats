using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace ClassNameStats
{
    static class Program
    {
        private const int TopX = 15;

        static int Main(string[] args)
        {
            if (args.Length != 1 || !Directory.Exists(args[0]))
            {
                Console.WriteLine("Usage:");
                Console.WriteLine($"\t{Assembly.GetEntryAssembly().GetName().Name}.exe <path>");
                return -1;
            }

            try
            {
                string path = args[0];
                var pattern = new Regex(
                    @"(?<type>class|interface)\s(?<name>\w+)",
                    RegexOptions.ExplicitCapture | RegexOptions.Compiled);

                var allTypes = Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories)
                    .AsParallel()
                    .Where(filePath => filePath.IndexOf("test", StringComparison.OrdinalIgnoreCase) == -1)
                    .Select(filePath =>
                        File.ReadLines(filePath)
                            .Select(line => pattern.Match(line))
                            .Where(m => m.Success)
                            .Select(m => new
                            {
                                IsInterface = m.Groups["type"].Value == "interface",
                                Name = m.Groups["name"].Value
                            })
                            .FirstOrDefault()
                    )
                    .Where(type => type != null)
                    .Distinct()
                    .Select(type => new
                    {
                        IsInterface = type.IsInterface,
                        Name = type.Name,
                        Length = type.Name.Length,
                        Words = Split(type.Name)
                    })
                    .ToList();

                Console.WriteLine("Stats");
                Console.WriteLine();

                Console.WriteLine($"Total number of classes {allTypes.Count(t => !t.IsInterface)}");
                Console.WriteLine($"Total number of intefaces {allTypes.Count(t => t.IsInterface)}");

                Console.WriteLine();

                var topLongest = allTypes
                    .OrderByDescending(n => n.Length)
                    .Select(n => n.Name)
                    .Take(TopX);

                var topWords = allTypes
                    .SelectMany(n => n.Words.Where(w => w.Length > 3))
                    .GroupBy(k => k)
                    .Select(g => new { Word = g.Key, Count = g.Count() })
                    .OrderByDescending(w => w.Count)
                    .ThenBy(w => w.Word)
                    .Take(TopX);

                Console.WriteLine($"Top {TopX} Longest Names");
                Console.WriteLine("--------------------");
                foreach (var name in topLongest)
                {
                    Console.WriteLine(name);
                }

                Console.WriteLine();

                Console.WriteLine($"Top {TopX} Most Used Words");
                Console.WriteLine("----------------------");
                foreach (var word in topWords)
                {
                    Console.WriteLine($"{word.Count,6} {word.Word}");
                }

                Console.WriteLine();
                return 0;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                return -2;
            }
        }

        private static string[] Split(string name)
        {
            if (String.IsNullOrWhiteSpace(name))
            {
                return new string[0];
            }
            else if (name.Length == 1)
            {
                return new string[] { name };
            }

            List<string> parts = new List<string>();
            char ch;
            int i = 0;
            int startIndex = 0;
            bool lastCharIsUpper = false;

            while (i < name.Length)
            {
                ch = name[i];

                if (Char.IsUpper(ch))
                {
                    if (!lastCharIsUpper && startIndex < i)
                    {
                        parts.Add(name.Substring(startIndex, i - startIndex));
                        startIndex = i;
                    }

                    lastCharIsUpper = true;
                }
                else
                {
                    if (lastCharIsUpper && i - startIndex - 1 > 0)
                    {
                        parts.Add(name.Substring(startIndex, i - startIndex - 1));
                        startIndex = i - 1;
                    }

                    lastCharIsUpper = false;
                }

                i += 1;
            }

            if (i >= startIndex)
            {
                parts.Add(name.Substring(startIndex));
            }

            return parts.ToArray();
        }
    }
}
