using Microsoft.Extensions.Configuration;

namespace ai_parser_2026
{
    public class AppConfig
    {
        private readonly IConfigurationRoot _config;

        public AppConfig()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json", optional: false)
                .Build();
        }

        public string OllamaUri => _config["Ollama:Uri"];
        public string EmbeddingModel => _config["Ollama:EmbeddingModel"];
        public string AnalysisModel => _config["Ollama:AnalysisModel"];
        public string DatabaseConnectionString => _config["Database:ConnectionString"];
        public string[] TargetChats => _config.GetSection("Telegram:TargetChats").Get<string[]>();

        // Telegram конфигурация
        public string TelegramApiId => _config["Telegram:ApiId"];
        public string TelegramApiHash => _config["Telegram:ApiHash"];
        public string TelegramPhoneNumber => _config["Telegram:PhoneNumber"];
        public string ExportedChatsFolder { get; set; }
        public int BatchSize { get; set; } = 20;
    }
}