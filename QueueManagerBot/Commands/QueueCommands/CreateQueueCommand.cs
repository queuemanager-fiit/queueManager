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

            var controllerUser = new ControllerUser(httpClient, apiBaseUrl);
            var user = await controllerUser.GetUser(msg.Chat.Id); 

            if (user == null)
            {
                await Bot.SendMessage(msg.Chat.Id, "Ошибка при получении данных пользователя");
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
                    else
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Воспользуйтесь клавитурой ниже");
                        return;
                    }
                        

                    var categories = await controllerUser.GetCategoryList(QueuesData[msg.Chat.Id]["GroupId"]);

                    if (categories == null || !categories.Any())
                    {
                        await Bot.SendMessage(
                            msg.Chat.Id, "Для вашей группы нет доступных категорий, создайте или попросите админа создать её", 
                            replyMarkup: new ReplyKeyboardRemove()
                            );
                        StateManager.SetState(msg.Chat.Id, UserState.None);
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
                        var parts = msg.Text.Split(' ', StringSplitOptions.RemoveEmptyEntries);
                        
                        var timePart = parts[0];
                        var datePart = parts[1];
                        
                        if (!TimeSpan.TryParseExact(timePart, "hh\\:mm", CultureInfo.InvariantCulture, out TimeSpan time))
                            throw new FormatException();
                        
                        var utcPlus5Offset = TimeSpan.FromHours(5);
                        var nowUtc = DateTime.UtcNow;
                        var nowUtcPlus5 = nowUtc + utcPlus5Offset;
                        
                        var dateWithYear = $"{datePart}.{nowUtcPlus5.Year}";
                        if (!DateTime.TryParseExact(dateWithYear, "dd.MM.yyyy", 
                            CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime parsedDate))
                            throw new FormatException();
                        
                        parsedDate = parsedDate.Date.Add(time);
                        
                        if (parsedDate < nowUtcPlus5)
                        {
                            var dateWithNextYear = $"{datePart}.{nowUtcPlus5.Year + 1}";
                            DateTime.TryParseExact(dateWithNextYear, "dd.MM.yyyy", 
                                CultureInfo.InvariantCulture, DateTimeStyles.None, out parsedDate);
                            parsedDate = parsedDate.Date.Add(time);
                        }
                        
                        var dateUtcPlus5 = new DateTimeOffset(parsedDate, utcPlus5Offset);
                        date = dateUtcPlus5.ToUniversalTime();
                    }
                    catch
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Неверный формат даты", replyMarkup: new ReplyKeyboardRemove());
                        return;
                    }
                    
                    var queue = new WebApi.Controllers.BotEventController.CreationDto(
                        QueuesData[msg.Chat.Id]["GroupId"],
                        QueuesData[msg.Chat.Id]["QueueCategory"],
                        date);
                        
                    var response = await controllerUser.CreateQueue(queue);

                    if (response)
                    {
                        QueuesData.Remove(msg.Chat.Id);
                        await Bot.SendMessage(msg.Chat.Id, "Очередь успешно создана", replyMarkup: new ReplyKeyboardRemove());
                        StateManager.SetState(msg.Chat.Id, UserState.None);
                    }
                    else
                    {
                        await Bot.SendMessage(msg.Chat.Id, $"Ошибка сохранения", replyMarkup: new ReplyKeyboardRemove());
                        StateManager.SetState(msg.Chat.Id, UserState.None);
                    }
                    break;
            }
        }

        public async Task HandleCategoryCallback(string categoryCallback, long tgId)
        {
            var categoryName = categoryCallback.Remove(0, "select_category_".Length);
            QueuesData[tgId]["QueueCategory"] = categoryName;
            await Bot.SendMessage(tgId, @"Введите время и дату в формате: ЧЧ:ММ ДД.ММ
Пример: 19:00 22.12 — 22 декабря в 19:00");
            StateManager.SetState(tgId, UserState.WaitingForQueueDate);
        }
    }
}
