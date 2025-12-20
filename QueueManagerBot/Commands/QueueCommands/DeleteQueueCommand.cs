using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Types.ReplyMarkups;

namespace QueueManagerBot
{
    public class DeleteQueueCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }
        private readonly HttpClient httpClient;
        private readonly string apiBaseUrl;

        public DeleteQueueCommand(
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
            switch (StateManager.GetState(msg.Chat.Id))
            {
                case UserState.None:
                    var tgID = new { msg.Chat.Id };
                    var userResponse = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/users/get-user", tgID);

                    if (!userResponse.IsSuccessStatusCode)
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Ошибка при получении данных пользователя");
                        return;
                    }

                    var user = await userResponse.Content.ReadFromJsonAsync<WebApi.Controllers.BotUserController.BotUserDto>();

                    if (user.IsAdmin)
                    {
                        var groupCode = new { user.GroupCode };
                        var subGroupCode = new { user.SubGroupCode };

                        var groupQueuesResponse = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/events/events-list-created-by", groupCode);
                        var subGroupQueuesResponse = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/events/events-list-created-by", subGroupCode);

                        var groupQueues = await groupQueuesResponse.Content.ReadFromJsonAsync<List<WebApi.Controllers.BotEventController.BotEventDto>>();
                        var subGroupQueues = await subGroupQueuesResponse.Content.ReadFromJsonAsync<List<WebApi.Controllers.BotEventController.BotEventDto>>();

                        var allQueues = new List<WebApi.Controllers.BotEventController.BotEventDto>();
                        if (groupQueues != null) allQueues.AddRange(groupQueues);
                        if (subGroupQueues != null) allQueues.AddRange(subGroupQueues);

                        if (!allQueues.Any())
                        {
                            await Bot.SendMessage(msg.Chat.Id, "У вас нет активных очередей");
                            StateManager.SetState(msg.Chat.Id, UserState.None);
                            return;
                        }

                        var buttons = new List<InlineKeyboardButton>();
                        foreach (var queue in allQueues)
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData(
                                text: $"{queue.OccurredOn} {queue.Category}",
                                callbackData: $"delete_queue_{queue.EventId}"
                            ));
                        }
                        var keyboard = new InlineKeyboardMarkup(buttons);
                        await Bot.SendMessage(msg.Chat.Id, "Выберете очередь для удаления", replyMarkup: keyboard);
                        StateManager.SetState(msg.Chat.Id, UserState.WaitingForQueueNameToDelete);
                    }
                    else
                    {
                        await Bot.SendMessage(msg.Chat.Id, "У вас нет прав для удаления очереди, это может сделать только админ");
                    }
                    break;
            }
        }
    }
}