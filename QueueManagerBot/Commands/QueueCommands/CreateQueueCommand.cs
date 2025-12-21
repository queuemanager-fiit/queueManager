using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using System.Net.Http.Json;
using WebApi.Controllers;
using Microsoft.Extensions.Configuration;
using System.Globalization;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;

namespace QueueManagerBot
{
    public class CreateQueueCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }
        public Dictionary<long, Dictionary<string, string>> QueuesData { get; } = new Dictionary<long, Dictionary<string, string>>();
        private readonly HttpClient httpClient;
        private readonly string apiBaseUrl;

        public CreateQueueCommand(
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
                UserState.WaitingForQueueCategory,
                UserState.WaitingForGroupId,
                UserState.WaitingForQueueDate
            };

            httpClient = httpClientFactory.CreateClient("ApiClient");
            apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:5001";
        }

        public bool CanExecute(Message msg, UserState state)
        {
            return (msg.Text == Name && state == UserState.None) || (state != UserState.None && AllowedStates.Contains(state));
        }

        public async Task Execute(Message msg)
        {
            if (!QueuesData.ContainsKey(msg.Chat.Id))
            {
                QueuesData.Add(msg.Chat.Id, new Dictionary<string, string>());
                QueuesData[msg.Chat.Id].Add("QueueCategory", "");
                QueuesData[msg.Chat.Id].Add("GroupId", "");
                QueuesData[msg.Chat.Id].Add("QueueDate", "");   
            }

            var date = new DateTimeOffset();

            var userResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/users/user-info?telegramId={msg.Chat.Id}");

            if (!userResponse.IsSuccessStatusCode)
            {
                await Bot.SendMessage(msg.Chat.Id, "Ошибка при получении данных пользователя");
                return;
            }

            var user = await userResponse.Content.ReadFromJsonAsync<WebApi.Controllers.BotUserController.InfoUserDto>();
            if (!user.IsAdmin)
            {
                await Bot.SendMessage(msg.Chat.Id, "Создавать очереди может только админ");
                return;
            }


            switch (StateManager.GetState(msg.Chat.Id))
            {
                case UserState.None:
                    await Bot.SendMessage(
                        msg.Chat.Id, 
                        "Для кого вы хотите создать очередь?", 
                        replyMarkup : new string[] { "Для всей группы", "Для своей половинки" }
                        );
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForGroupId);
                    break;
                    

                case UserState.WaitingForGroupId:
                    if (msg.Text == "Для всей группы")
                        QueuesData[msg.Chat.Id]["GroupId"] = user.GroupCode;
                    else if (msg.Text == "Для своей половинки")
                        QueuesData[msg.Chat.Id]["GroupId"] = user.SubGroupCode;
                        


                    var categoriesResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/groups/category-list?groupCode={QueuesData[msg.Chat.Id]["GroupId"]}");
    
                    if (!categoriesResponse.IsSuccessStatusCode)
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Ошибка при получении категорий");
                        return;
                    }
                    
                    var categories = await categoriesResponse.Content.ReadFromJsonAsync<List<string>>();
                    Console.WriteLine(QueuesData[msg.Chat.Id]["GroupId"]);
                    if (categories == null || !categories.Any())
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Для вашей группы нет доступных категорий");
                        return;
                    }
                    
                    var buttons = new List<InlineKeyboardButton>();
                    
                    foreach (var category in categories)
                    {
                        buttons.Add(InlineKeyboardButton.WithCallbackData(
                            text: category,
                            callbackData: $"select_category_{category}"
                        ));
                    }
                    
                    var rows = new List<List<InlineKeyboardButton>>();
                    for (int i = 0; i < buttons.Count; i += 2)
                    {
                        var row = new List<InlineKeyboardButton>();
                        row.Add(buttons[i]);
                        
                        if (i + 1 < buttons.Count)
                            row.Add(buttons[i + 1]);
                        
                        rows.Add(row);
                    }
                    
                    var keyboard = new InlineKeyboardMarkup(rows);
                    
                    await Bot.SendMessage(
                        msg.Chat.Id,
                        "Выберите категорию для очереди:",
                        replyMarkup: keyboard
                    );
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForQueueCategory);
                    break;
                    

                case UserState.WaitingForQueueCategory:

                    break;

                case UserState.WaitingForQueueDate:
                    QueuesData[msg.Chat.Id]["QueueDate"] = msg.Text;
                    try
                    {
                        date = DateTimeOffset.ParseExact(
                        QueuesData[msg.Chat.Id]["QueueDate"] + "." + DateTime.Now.Year, 
                        "dd.MM.yyyy", 
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeUniversal
                    );
                    }
                    catch
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Неверный формат даты");
                        return;
                    }
                    

                    try
                    {
                        Console.WriteLine("1");
                        var queue = new WebApi.Controllers.BotEventController.CreationDto(
                            QueuesData[msg.Chat.Id]["GroupId"],
                            QueuesData[msg.Chat.Id]["QueueCategory"],
                            date);
                        Console.WriteLine("2");
                        var response = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/events/create-queue", queue);
                        Console.WriteLine(response.StatusCode);


                        if (response.IsSuccessStatusCode)
                        {
                            
                            QueuesData.Remove(msg.Chat.Id);
                            Console.WriteLine("4");
                            await Bot.SendMessage(msg.Chat.Id, "Очередь успешно создана");
                            StateManager.SetState(msg.Chat.Id, UserState.None);
                        }
                        else
                        {
                            var errorContent = await response.Content.ReadAsStringAsync();
                            await Bot.SendMessage(msg.Chat.Id, $"Ошибка сохранения: {response.StatusCode}\n{errorContent}");
                        }
                    }
                    catch (Exception ex)
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Произошла непредвиденная ошибка");
                        Console.WriteLine(ex.Message.Substring(0,500));

                    }
                    break;
            }
        }

        public async Task HandleCategoryCallback(string categoryCallback, long tgId)
        {
            var categoryName = categoryCallback.Remove(0, "select_category_".Length);
            QueuesData[tgId]["QueueCategory"] = categoryName;
            await Bot.SendMessage(tgId, "Введите дату категории в формате ДД.ММ");
            StateManager.SetState(tgId, UserState.WaitingForQueueDate);
        }
    }
}
