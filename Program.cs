using HtmlAgilityPack; // Need to install HtmlAgilityPack NuGet package
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;



namespace ai_parser_2026
{
    class Program
    {
        static async Task Main(string[] args)
        {
            List<string> result = new List<string>();


            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("Запуск парсера экспортированных HTML чатов (телефоны, email, имена, даты, суммы)\n");

            try
            {
                // 1. Загружаем конфигурацию (предполагается, что AppConfig уже существует)
                var config = new AppConfig();
                string chatFolder = config.ExportedChatsFolder ?? "C:\\C# projects\\ai_parser_2026\\Chats\\"; // путь к папке с HTML файлами
                int batchSize = config.BatchSize; // количество сообщений на один промт

                if (!Directory.Exists(chatFolder))
                {
                    Console.WriteLine($"Папка '{chatFolder}' не найдена. Укажите правильный путь в настройках.");
                    return;
                }

                // 2. Получаем все HTML файлы в папке
                var htmlFiles = Directory.GetFiles(chatFolder, "*.html");
                if (htmlFiles.Length == 0)
                {
                    Console.WriteLine($"В папке '{chatFolder}' нет HTML файлов.");
                    return;
                }

                // 3. Создаём парсер (ваш существующий OllamaParser)
                var parser = new OllamaParser(); // или OllamaParser2

                // 4. Для каждого файла обрабатываем сообщения
                foreach (var file in htmlFiles)
                {
                    Console.WriteLine($"\n📁 Обработка файла: {Path.GetFileName(file)}");

                    // Парсим HTML и получаем список сообщений
                    var messages = HtmlMessageParser.Parse(file);

                    if (messages.Count == 0)
                    {
                        Console.WriteLine("   Нет текстовых сообщений для анализа.");
                        continue;
                    }

                    Console.WriteLine($"   Всего сообщений: {messages.Count}");

                    // 5. Разбиваем на батчи по batchSize
                    var batches = messages.Select((msg, index) => new { msg, index })
                                          .GroupBy(x => x.index / batchSize)
                                          .Select(g => g.Select(x => x.msg).ToList())
                                          .ToList();

                    Console.WriteLine($"   Батчей (по {batchSize} сообщений): {batches.Count}");

                    // 6. Обрабатываем каждый батч
                    for (int i = 0; i < batches.Count; i++)
                    {
                        var batch = batches[i];
                        Console.WriteLine($"\n   --- Батч {i + 1} ---");

                        // Формируем текст для отправки: объединяем сообщения с разделителем
                        string combinedText = string.Join("\n---\n", batch.Select(m => $"[{m.Sender}]: {m.Text}"));

                        // Отправляем на анализ
                        var extracted = await parser.ParseAllAsync(combinedText);

                        result.Add(extracted.ToString());

                        // Выводим результат
                        Console.WriteLine($"   🔍 Найдено: {extracted}");
                    }
                }

                Console.WriteLine("\n✅ Анализ завершён!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Ошибка: {ex.Message}");
            }

            Console.WriteLine("\nНажмите любую клавишу для выхода...");
            Console.ReadKey();
        }


        private static (HashSet<string> names, HashSet<string> phones, HashSet<string> emails, HashSet<string> dates, HashSet<string> amounts)
            ParseExtracted(string extracted)
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var phones = new HashSet<string>(StringComparer.Ordinal);
            var emails = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            var dates = new HashSet<string>(StringComparer.Ordinal);
            var amounts = new HashSet<string>(StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(extracted))
                return (names, phones, emails, dates, amounts);

            var lines = extracted.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var line in lines)
            {
                if (line.StartsWith("Имена:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Substring(6).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var name = part.Trim();
                        if (!string.IsNullOrEmpty(name) && !IsStopWord(name))
                            names.Add(name);
                    }
                }
                else if (line.StartsWith("Телефоны:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Substring(9).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var phone = part.Trim();
                        if (!string.IsNullOrEmpty(phone) && !phone.Equals("не найдены", StringComparison.OrdinalIgnoreCase))
                            phones.Add(phone);
                    }
                }
                else if (line.StartsWith("Email:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Substring(6).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var email = part.Trim();
                        if (!string.IsNullOrEmpty(email) && !email.Equals("не найдены", StringComparison.OrdinalIgnoreCase))
                            emails.Add(email);
                    }
                }
                else if (line.StartsWith("Даты:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Substring(5).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var date = part.Trim();
                        if (!string.IsNullOrEmpty(date) && !date.Equals("не найдены", StringComparison.OrdinalIgnoreCase))
                            dates.Add(date);
                    }
                }
                else if (line.StartsWith("Суммы:", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = line.Substring(6).Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
                    foreach (var part in parts)
                    {
                        var amount = part.Trim();
                        if (!string.IsNullOrEmpty(amount) && !amount.Equals("не найдены", StringComparison.OrdinalIgnoreCase))
                            amounts.Add(amount);
                    }
                }
                // Строки, не начинающиеся с ключевых слов (заголовки, разделители), игнорируются
            }

            return (names, phones, emails, dates, amounts);
        }

        /// <summary>
        /// Проверяет, является ли строка стоп-словом (мусорным значением).
        /// </summary>
        private static bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                "неизвестный", "unknown", "undefined", "null", "n/a", "не найдены"
            };
            return stopWords.Contains(word.Trim());
        }
    }
    /// <summary>
    /// Простой класс для хранения информации о сообщении.
    /// </summary>
    public class MessageInfo
    {
        public string Sender { get; set; }
        public string Text { get; set; }
        public string Date { get; set; } // не обязательно, но можно сохранять
    }

    /// <summary>
    /// Парсер HTML экспорта Telegram.
    /// </summary>
    public static class HtmlMessageParser
    {
        public static List<MessageInfo> Parse(string filePath)
        {
            var messages = new List<MessageInfo>();

            var doc = new HtmlDocument();
            doc.Load(filePath, Encoding.UTF8);

            // Все сообщения находятся в div с классом "message default clearfix"
            var messageNodes = doc.DocumentNode.SelectNodes("//div[contains(@class, 'message') and contains(@class, 'default') and contains(@class, 'clearfix')]");

            if (messageNodes == null)
                return messages;

            foreach (var node in messageNodes)
            {
                // Извлекаем отправителя
                var senderNode = node.SelectSingleNode(".//div[@class='from_name']");
                string sender = senderNode?.InnerText.Trim() ?? "Unknown";

                // Извлекаем текст сообщения
                var textNode = node.SelectSingleNode(".//div[@class='text']");
                if (textNode == null)
                    continue; // пропускаем сообщения без текста (медиа, стикеры и т.п.)

                string text = textNode.InnerText.Trim();

                // Извлекаем дату (опционально)
                var dateNode = node.SelectSingleNode(".//div[@class='pull_right date details']");
                string date = dateNode?.InnerText.Trim() ?? "";

                messages.Add(new MessageInfo
                {
                    Sender = sender,
                    Text = text,
                    Date = date
                });
            }

            return messages;
        }
    }

    // Предполагается, что класс AppConfig уже существует и содержит нужные свойства
    // public class AppConfig
    // {
    //     public string ExportedChatsFolder { get; set; }
    //     public int BatchSize { get; set; }
    // }

    // Предполагается, что класс OllamaParser уже существует и имеет метод ParseAllAsync(string)
    // public class OllamaParser
    // {
    //     public async Task<string> ParseAllAsync(string input) { ... }
    // }
}