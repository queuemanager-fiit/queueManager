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
                    var userResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/users/user-info?telegramId={msg.Chat.Id}");

                    if (!userResponse.IsSuccessStatusCode)
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Ошибка при получении данных пользователя");
                        return;
                    }
                    Console.WriteLine($"3");
                    var user = await userResponse.Content.ReadFromJsonAsync<WebApi.Controllers.BotUserController.InfoUserDto>();

                    if (user.IsAdmin)
                    {
                        var groupCode = new { user.GroupCode };
                        var subGroupCode = new { user.SubGroupCode };
                        Console.WriteLine($"1");
                        var groupQueuesResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/events/events-for-group?groupCode={groupCode}");
                        var subGroupQueuesResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/events/events-for-group?groupCode={subGroupCode}");


                        Console.WriteLine($"{groupCode}, {subGroupCode}");
                        var groupQueues = await groupQueuesResponse.Content.ReadFromJsonAsync<List<WebApi.Controllers.BotEventController.BotEventDto>>();
                        var subGroupQueues = await subGroupQueuesResponse.Content.ReadFromJsonAsync<List<WebApi.Controllers.BotEventController.BotEventDto>>();
                        Console.WriteLine($"2");
                        var allQueues = new List<WebApi.Controllers.BotEventController.BotEventDto>();
                        if (groupQueues != null) allQueues.AddRange(groupQueues);
                        if (subGroupQueues != null) allQueues.AddRange(subGroupQueues);
                        Console.WriteLine($"3");
                        if (!allQueues.Any())
                        {
                            await Bot.SendMessage(msg.Chat.Id, "У вас нет активных очередей");
                            StateManager.SetState(msg.Chat.Id, UserState.None);
                            return;
                        }
                        Console.WriteLine($"4");
                        var buttons = new List<InlineKeyboardButton>();
                        foreach (var queue in allQueues)
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData(
                                text: $"{queue.OccurredOn} {queue.Category}",
                                callbackData: $"delete_queue_{queue.EventId}"
                            ));
                        }
                        Console.WriteLine($"5");
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