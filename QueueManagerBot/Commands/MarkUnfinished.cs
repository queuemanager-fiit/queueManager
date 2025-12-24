using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using WebApi.Controllers;
using Telegram.Bot.Types.ReplyMarkups;

namespace QueueManagerBot
{
    public class MarkUnfinished : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }
        private readonly HttpClient httpClient;
        private readonly string apiBaseUrl;

        public MarkUnfinished(
            string name, 
            TelegramBotClient tgBot, 
            StateManager stateManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration) 
        {
            Name = name;
            Bot = tgBot;
            StateManager = stateManager;
            AllowedStates = new UserState[]
            {
                UserState.None,
            };

            httpClient = httpClientFactory.CreateClient("ApiClient");
            apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:5001";
        }

        public bool CanExecute(Message msg, UserState state)
        {
            return msg.Text == Name && AllowedStates.Contains(state);
        }

        public async Task Execute(Message msg)
        {
            var controllerUser = new ControllerUser(httpClient, apiBaseUrl);
            var user = await controllerUser.GetUser(msg.Chat.Id);
            var groupQueues = await controllerUser.GetQueueListForGroup(user.GroupCode);
            var subGroupQueues = await controllerUser.GetQueueListForGroup(user.SubGroupCode);

            var allQueues = new List<WebApi.Controllers.BotEventController.BotEventDto>();
            if (groupQueues != null) allQueues.AddRange(groupQueues);
            if (subGroupQueues != null) allQueues.AddRange(subGroupQueues);

            if (!allQueues.Any())
            {
                await Bot.SendMessage(msg.Chat.Id, "В вашей группе нет очередей");
                return;
            }

            var keyboardRows = new List<IEnumerable<InlineKeyboardButton>>();
    
            foreach (var queue in allQueues)
            {
                var dateStr = queue.OccurredOn.ToString("dd.MM HH:mm");
                
                var buttonText = $"{queue.Category} - {dateStr}";
                
                var callbackData = $"queue_info_{queue.EventId}";
                
                keyboardRows.Add(new[]
                {
                    InlineKeyboardButton.WithCallbackData(buttonText, callbackData)
                });
            }

            var inlineKeyboard = new InlineKeyboardMarkup(keyboardRows);

            await Bot.SendMessage(
                chatId: msg.Chat.Id,
                text: "📋 Доступные очереди:\nВыберите очередь для просмотра деталей:",
                replyMarkup: inlineKeyboard
            );
        }
    }
}