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
            // var isAdmin = db.IsAdmin(msg.Chat.Id);
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
            switch (StateManager.GetState(msg.Chat.Id))
            {
                case UserState.None:
                    await Bot.SendMessage(msg.Chat.Id, "Введите название категории");
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForQueueCategory);
                    break;  
                case UserState.WaitingForQueueCategory:
                    QueuesData[msg.Chat.Id]["QueueCategory"] = msg.Text;
                    await Bot.SendMessage(
                        msg.Chat.Id, 
                        "Для кого вы хотите создать очередь?", 
                        replyMarkup : new string[] { "Для всей группы", "Для своей половинки" }
                        );
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForGroupId);
                    break;

                case UserState.WaitingForGroupId:
                    var tgID = new { msg.Chat.Id };
                    var userResponse = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/users/get-user", tgID);

                    if (!userResponse.IsSuccessStatusCode)
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Ошибка при получении данных пользователя");
                        return;
                    }

                    var user = await userResponse.Content.ReadFromJsonAsync<WebApi.Controllers.BotUserController.BotUserDto>();

                    if (msg.Text == "Для всей группы")
                        QueuesData[msg.Chat.Id]["GroupId"] = user.GroupCode;
                    else if (msg.Text == "Для своей половинки")
                        QueuesData[msg.Chat.Id]["GroupId"] = user.SubGroupCode;
                    await Bot.SendMessage(msg.Chat.Id, "Введите дату категории в формате ДД.ММ");
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForQueueDate);
                    break;

                case UserState.WaitingForQueueDate:
                    QueuesData[msg.Chat.Id]["QueueDate"] = msg.Text;
                    try
                    {
                        date = DateTimeOffset.ParseExact(
                        QueuesData[msg.Chat.Id]["QueueDate"] + "." + DateTime.Now.Year, 
                        "dd.MM.yyyy", 
                        CultureInfo.InvariantCulture,
                        DateTimeStyles.AssumeLocal
                    );
                    }
                    catch
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Неверный формат даты");
                        return;
                    }
                    

                    try
                    {
                        var queue = new WebApi.Controllers.BotEventController.CreationDto(
                            QueuesData[msg.Chat.Id]["GroupId"],
                            QueuesData[msg.Chat.Id]["QueueCategory"],
                            date);
                        var response = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/events/create-queue", queue);

                        if (response.IsSuccessStatusCode)
                        {
                            QueuesData.Remove(msg.Chat.Id);
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
                        Console.WriteLine(ex.Message);
                    }
                    break;
            }
        }
    }
}
