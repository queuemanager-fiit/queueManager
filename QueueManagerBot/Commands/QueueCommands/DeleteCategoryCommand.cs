using Telegram.Bot;
using Telegram.Bot.Types;
using Microsoft.Extensions.Configuration;
using System.Runtime.CompilerServices;
using System.Net.Http.Json;
using System.ComponentModel.DataAnnotations;
using Telegram.Bot.Types.ReplyMarkups;

namespace QueueManagerBot
{
    public class DeleteCategoryCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }
        private readonly HttpClient httpClient;
        private readonly string apiBaseUrl;

        public DeleteCategoryCommand(
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
                        var groupCatsResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/groups/category-list?groupCode={groupCode}");
                        var subGroupCatsResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/groups/category-list?groupCode={subGroupCode}");


                        Console.WriteLine($"{groupCode}, {subGroupCode}");
                        var groupCats = await groupCatsResponse.Content.ReadFromJsonAsync<List<string>>();
                        var subGroupCats = await subGroupCatsResponse.Content.ReadFromJsonAsync<List<string>>();
                        Console.WriteLine($"2");
                        var allCats = new List<string>();
                        if (groupCats != null) allCats.AddRange(groupCats);
                        if (subGroupCats != null) allCats.AddRange(subGroupCats);
                        Console.WriteLine($"3");
                        if (!allCats.Any())
                        {
                            await Bot.SendMessage(msg.Chat.Id, "У вас нет активных категорий");
                            StateManager.SetState(msg.Chat.Id, UserState.None);
                            return;
                        }
                        Console.WriteLine($"4");
                        var buttons = new List<InlineKeyboardButton>();
                        foreach (var cat in allCats)
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData(
                                text: $"{cat}",
                                callbackData: $"delete_category_{cat}"
                            ));
                        }
                        Console.WriteLine($"5");
                        var keyboard = new InlineKeyboardMarkup(buttons);
                        await Bot.SendMessage(msg.Chat.Id, "Выберете категорию для удаления", replyMarkup: keyboard);
                        StateManager.SetState(msg.Chat.Id, UserState.WaitingForQueueNameToDelete);
                    }
                    else
                    {
                        await Bot.SendMessage(msg.Chat.Id, "У вас нет прав для удаления категории, это может сделать только админ");
                    }
                    break;
            }
        }
    }
}