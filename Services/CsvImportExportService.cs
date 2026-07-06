using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace FoodEnterpriseIMS.Services
{
    /// <summary>
    /// 通用CSV导入导出工具服务。
    /// </summary>
    public static class CsvImportExportService
    {
        public static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            var sb = new StringBuilder();
            var inQuotes = false;

            for (var i = 0; i < line.Length; i++)
            {
                var c = line[i];
                if (inQuotes)
                {
                    if (c == '"')
                    {
                        if (i + 1 < line.Length && line[i + 1] == '"')
                        {
                            sb.Append('"');
                            i++;
                        }
                        else
                        {
                            inQuotes = false;
                        }
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
                else
                {
                    if (c == '"')
                    {
                        inQuotes = true;
                    }
                    else if (c == ',')
                    {
                        result.Add(sb.ToString());
                        sb.Clear();
                    }
                    else
                    {
                        sb.Append(c);
                    }
                }
            }

            result.Add(sb.ToString());
            return result;
        }

        public static string EscapeCsv(string? value)
        {
            if (value == null)
            {
                return "\"\"";
            }

            if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
            {
                return "\"" + value.Replace("\"", "\"\"") + "\"";
            }

            return "\"" + value + "\"";
        }

        public static Dictionary<string, int> BuildHeaderMap(List<string> header)
        {
            var map = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            for (var i = 0; i < header.Count; i++)
            {
                var key = (header[i] ?? string.Empty).Trim().Replace("\uFEFF", string.Empty);
                if (!string.IsNullOrWhiteSpace(key))
                {
                    map[key] = i;
                }
            }

            return map;
        }

        public static string GetMappedValue(List<string> values, Dictionary<string, int> map, params string[] keys)
        {
            foreach (var key in keys)
            {
                if (map.TryGetValue(key, out var idx) && idx >= 0 && idx < values.Count)
                {
                    return values[idx]?.Trim() ?? string.Empty;
                }
            }

            return string.Empty;
        }

        public static List<List<string>> ReadCsvRows(string filePath)
        {
            var lines = File.ReadAllLines(filePath, Encoding.UTF8);
            return lines.Select(ParseCsvLine).ToList();
        }

        public static void WriteCsv(string filePath, IEnumerable<string> headers, IEnumerable<IEnumerable<string?>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", headers.Select(EscapeCsv)));
            foreach (var row in rows)
            {
                sb.AppendLine(string.Join(",", row.Select(EscapeCsv)));
            }

            File.WriteAllText(filePath, sb.ToString(), Encoding.UTF8);
        }
    }
}
