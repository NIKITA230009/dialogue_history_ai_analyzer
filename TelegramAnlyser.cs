using OllamaSharp.Models.Chat;
using TL;
using WTelegram;
using Microsoft.Extensions.Configuration;
using ai_parser_2026;
using OllamaSharp.Models;
using Message = TL.Message;

namespace ai_parser_2026
{
    public class TelegramService
    {
        private readonly Client _client;
        private readonly AppConfig _config;

        public TelegramService(AppConfig config)
        {
            _config = config;
            _client = new Client(ConfigCallback);
        }

        private string ConfigCallback(string what)
        {
            switch (what)
            {
                case "api_id": return _config.TelegramApiId;
                case "api_hash": return _config.TelegramApiHash;
                case "phone_number": return _config.TelegramPhoneNumber;
                case "verification_code":
                    Console.Write("Введите код подтверждения из Telegram: ");
                    return Console.ReadLine();
                case "password":
                    Console.Write("Введите пароль двухфакторной аутентификации: ");
                    return Console.ReadLine();
                default: return null;
            }
        }

        public async Task<User> LoginAsync()
        {
            var myself = await _client.LoginUserIfNeeded();
            Console.WriteLine($"Авторизован как: {myself} (id {myself.id})");
            return myself;
        }

        public async Task<List<ChatBase>> GetTargetChatsAsync(string[] chatNames)
        {
            var chats = await _client.Messages_GetAllChats();
            var result = new List<ChatBase>();

            foreach (var (id, chat) in chats.chats)
            {
                if (chat.IsActive && chatNames.Any(name =>
                    chat.Title?.Contains(name, StringComparison.OrdinalIgnoreCase) == true))
                {
                    Console.WriteLine($"Найден целевой чат: {chat.Title} (ID: {id})");
                    result.Add(chat);
                }
            }

            return result;
        }

        public async Task<List<Message>> GetChatMessagesAsync(ChatBase chat, int limit = 100)
        {
            var messages = new List<Message>();
            InputPeer peer = chat;

            var history = await _client.Messages_GetHistory(peer, limit: limit);

            foreach (var msgBase in history.Messages)
            {
                if (msgBase is Message msg && !string.IsNullOrEmpty(msg.message))
                {
                    messages.Add(msg);
                }
            }

            Console.WriteLine($"Получено {messages.Count} текстовых сообщений");
            return messages;
        }

        public async Task<string> GetSenderNameAsync(Message msg, ChatBase chat)
        {
            if (msg.from_id == null) return "Unknown";

            try
            {
                var inputUser = new InputUserFromMessage
                {
                    user_id = msg.From.ID,
                    msg_id = msg.ID,
                    peer = chat.ToInputPeer()
                };

                var users = await _client.Users_GetUsers(new[] { inputUser });
                var userBase = users.FirstOrDefault();

                // Приводим к типу User, где есть first_name и last_name
                if (userBase is User user)
                {
                    return $"{user.first_name} {user.last_name}".Trim();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }

            return $"User_{msg.From.ID}";
        }
    }
    }