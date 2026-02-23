using ai_parser_2026.Entities;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ai_parser_2026
{
    class OllamaParser : RegularParser
    {
        private readonly HttpClient _httpClient;
        private readonly string _model;

        public OllamaParser(string model = "mistral", string baseUrl = "http://localhost:11434")
        {
            _httpClient = new HttpClient();
            _httpClient.BaseAddress = new Uri(baseUrl);
            _httpClient.Timeout = TimeSpan.FromSeconds(30);
            _model = model;
        }

        public async Task<string> GenerateAsync(string prompt)
        {
            try
            {
                var request = new
                {
                    model = _model,
                    prompt = prompt,
                    stream = false,
                    options = new
                    {
                        temperature = 0.1, // Еще меньше креативности для точности
                        num_predict = 100
                    }
                };

                var content = new StringContent(
                    JsonSerializer.Serialize(request),
                    Encoding.UTF8,
                    "application/json"
                );

                var response = await _httpClient.PostAsync("/api/generate", content);
                response.EnsureSuccessStatusCode();

                var jsonResponse = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(jsonResponse);

                return doc.RootElement.GetProperty("response").GetString();
            }
            catch (Exception ex)
            {
                return $"Ошибка: {ex.Message}";
            }
        }

        public override async Task<ExtractedData> ParseAllAsync(string text)
        {
            var result = await base.ParseAllAsync(text);



            string namePrompt = $@"Твоя задача - найти ВСЕ имена людей в тексте и вернуть ТОЛЬКО список имен через запятую.

ВАЖНО: Не добавляй никаких пояснений, диалогов или лишнего текста. Только имена через запятую.

Текст для анализа: {text}";

            string nameResponse = await GenerateAsync(namePrompt);
            result.Names = ParseNames(nameResponse);

            return result;
        }

        private List<string> ParseNames(string response)
        {
            var names = new List<string>();

            if (string.IsNullOrWhiteSpace(response))
                return names;

            // Удаляем возможные пояснения в начале и конце
            response = CleanNameResponse(response);

            // Разделяем по запятой
            var items = response.Split(new[] { ',', '\n', ';' }, StringSplitOptions.RemoveEmptyEntries);

            foreach (var item in items)
            {
                // Чистим каждый элемент
                var name = CleanName(item);

                if (!string.IsNullOrWhiteSpace(name) && name.Length > 2)
                {
                    names.Add(name);
                }
            }

            return names.Distinct().ToList();
        }

        private string CleanNameResponse(string response)
        {
            // Удаляем фразы типа "Имена:", "Ответ:", "Вот имена:" и т.д.
            response = Regex.Replace(response, @"(?i)(имена:|ответ:|вот имена:|список имен:|результат:)", "");

            // Удаляем лишние пробелы и переносы
            response = Regex.Replace(response, @"\s+", " ").Trim();

            return response;
        }

        private string CleanName(string name)
        {
            // Удаляем лишние символы
            name = name.Trim()
                       .Trim('.', ',', '!', '?', ':', ';', '"', '\'', '(', ')', '[', ']', '{', '}')
                       .Trim();

            // Удаляем возможные числовые индексы (1., 2., и т.д.)
            name = Regex.Replace(name, @"^\d+\.\s*", "");

            return name;
        }
    }
}





