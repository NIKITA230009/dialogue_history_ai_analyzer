using System;
using System.Collections.Generic;
using System.Text;

namespace ai_parser_2026.Entities
{
    public class ExtractedData
    {
        public List<string> Names { get; set; } = new();
        public List<string> Phones { get; set; } = new();
        public List<string> Emails { get; set; } = new();

        public override string ToString()
        {
            var sb = new StringBuilder();
            sb.AppendLine("РЕЗУЛЬТАТЫ ПАРСИНГА:");
            sb.AppendLine(new string('─', 40));
            sb.AppendLine($"Имена: {FormatList(Names)}");
            sb.AppendLine($"Телефоны: {FormatList(Phones)}");
            sb.AppendLine($"Email: {FormatList(Emails)}");
            sb.AppendLine(new string('─', 40));
            return sb.ToString();
        }

        private string FormatList(List<string> list)
        {
            return list.Any() ? string.Join(", ", list) : "не найдены";
        }
    }
}
