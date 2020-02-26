using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DiagnosticCore.Portability.Helpers
{
    // https://github.com/dotnet/BenchmarkDotNet/blob/df6f915886788cadd70d8f43c4d9873c1ad9f1d1/src/BenchmarkDotNet/Helpers/SectionsHelper.cs
    internal static class SectionsHelper
    {
        public static Dictionary<string, string> ParseSection(string content, char separator)
        {
            var values = new Dictionary<string, string>();
            var list = content?.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            if (list != null)
                foreach (string line in list)
                    if (line.IndexOf(separator) != -1)
                    {
                        var lineParts = line.Split(separator);
                        if (lineParts.Length >= 2)
                            values[lineParts[0].Trim()] = lineParts[1].Trim();
                    }
            return values;
        }

        public static List<Dictionary<string, string>> ParseSections(string content, char separator)
        {
            // wmic doubles the carriage return character due to a bug.
            // Therefore, the * quantifier should be used to workaround it.
            return
                Regex.Split(content ?? "", "(\r*\n){2,}")
                    .Select(s => ParseSection(s, separator))
                    .Where(s => s.Count > 0)
                    .ToList();
        }
    }
}
