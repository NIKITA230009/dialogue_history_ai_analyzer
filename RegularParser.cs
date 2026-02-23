using ai_parser_2026.Entities;
using System.Text.RegularExpressions;


namespace ai_parser_2026
{
    class RegularParser
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;



        public virtual async Task<ExtractedData> ParseAllAsync(string text)
        {
            var result = new ExtractedData();

            // 1. Телефоны (регулярками)
            result.Phones = ExtractPhones(text);

            // 2. Email (регулярками)
            result.Emails = ExtractEmails(text);



            return result;
        }





        private List<string> ExtractPhones(string text)
        {
            var phones = new List<string>();

            var phonePatterns = new[]
            {
            @"\+7[\s\-]?\d{3}[\s\-]?\d{3}[\s\-]?\d{2}[\s\-]?\d{2}",  // +7 XXX XXX XX XX
            @"8[\s\-]?\d{3}[\s\-]?\d{3}[\s\-]?\d{2}[\s\-]?\d{2}",    // 8 XXX XXX XX XX
            @"7\d{10}",                                                // 7XXXXXXXXXX
            @"8\d{10}"                                                 // 8XXXXXXXXXX
        };

            foreach (var pattern in phonePatterns)
            {
                var matches = Regex.Matches(text, pattern);
                foreach (Match match in matches)
                {
                    phones.Add(match.Value);
                }
            }

            return phones.Distinct().ToList();
        }

        private List<string> ExtractEmails(string text)
        {
            var emails = new List<string>();
            var emailRegex = new Regex(@"[\w\.-]+@[\w\.-]+\.\w+", RegexOptions.IgnoreCase);
            var matches = emailRegex.Matches(text);

            foreach (Match match in matches)
            {
                emails.Add(match.Value);
            }

            return emails.Distinct().ToList();
        }
    }



}

