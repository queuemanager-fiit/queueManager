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
            var controllerUser = new ControllerUser(httpClient, apiBaseUrl);
            var user = await controllerUser.GetUser(msg.Chat.Id);

            if (user == null)
            {
                await Bot.SendMessage(msg.Chat.Id, "Произошла непредвиденная ошибка");
            }

            switch (StateManager.GetState(msg.Chat.Id))
            {
                case UserState.None:
                    if (user.IsAdmin)
                    {                 
                        var groupCats = await controllerUser.GetCategoryList(user.GroupCode);
                        var subGroupCats = await controllerUser.GetCategoryList(user.SubGroupCode);

                        var allCats = new List<string>();
                        if (groupCats != null) allCats.AddRange(groupCats);
                        if (subGroupCats != null) allCats.AddRange(subGroupCats);
                        
                        if (!allCats.Any())
                        {
                            await Bot.SendMessage(msg.Chat.Id, "У вас нет активных категорий");
                            StateManager.SetState(msg.Chat.Id, UserState.None);
                            return;
                        }
                        
                        var buttons = new List<InlineKeyboardButton>();
                        foreach (var cat in allCats)
                        {
                            buttons.Add(InlineKeyboardButton.WithCallbackData(
                                text: $"{cat}",
                                callbackData: $"delete_category_{cat}"
                            ));
                        }
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