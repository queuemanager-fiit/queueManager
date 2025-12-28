using System.Data.Common;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;
using WebApi.Controllers;
using System.Net.Http.Json;
using Microsoft.Extensions.Configuration;
using Table;

namespace QueueManagerBot
{
    public class AddCategoryCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }
        public Dictionary<long, Dictionary<string, string>> CategoriesData { get; } = new Dictionary<long, Dictionary<string, string>>();
        private readonly HttpClient httpClient;
        private readonly string apiBaseUrl;

        public AddCategoryCommand(
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
                UserState.WaitingForAutoOrNot,
                UserState.WaitingForNewCategoryName,
                UserState.WaitingForGroupIdCategory,

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
            var controllerUser = new ControllerUser(httpClient, apiBaseUrl);
            var user = await controllerUser.GetUser(msg.Chat.Id);

            if (user == null)
            {
                await Bot.SendMessage(msg.Chat.Id, "Ошибка при получении данных пользователя");
                return;
            }

            if (!user.IsAdmin)
            {
                await Bot.SendMessage(msg.Chat.Id, "Создавать категории может только админ");
                return;
            }

            if (!CategoriesData.ContainsKey(msg.Chat.Id))
            {
                CategoriesData.Add(msg.Chat.Id, new Dictionary<string, string>());
                CategoriesData[msg.Chat.Id].Add("IsAutomatic", "");
                CategoriesData[msg.Chat.Id].Add("CategotyName", "");
                CategoriesData[msg.Chat.Id].Add("GroupId", "");
            }


            switch (StateManager.GetState(msg.Chat.Id))
            {
                case UserState.None:
                    await Bot.SendMessage(
                        msg.Chat.Id,
                        "Хотите ли вы, чтобы категория была автоматической?",
                        replyMarkup: new string[] { "Да", "Нет" }
                        );
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForAutoOrNot);
                    break;
                case UserState.WaitingForAutoOrNot:
                    if (msg.Text == "Да")
                    {
                        CategoriesData[msg.Chat.Id]["IsAutomatic"] = "true";
                    }
                    else if (msg.Text == "Нет")
                        CategoriesData[msg.Chat.Id]["IsAutomatic"] = "";
                    else
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Воспользуйтесь клавитурой ниже");
                        return;
                    }
                    await Bot.SendMessage(
                        msg.Chat.Id,
                        "Для кого хотите создать категорию?",
                        replyMarkup: new string[] { "Для всей группы", "Для своей половинки" }
                        );
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForNewCategoryName);
                    break;
                case UserState.WaitingForNewCategoryName:
                    if (msg.Text == "Для всей группы")
                        CategoriesData[msg.Chat.Id]["GroupId"] = user.GroupCode;
                    else if (msg.Text == "Для своей половинки")
                        CategoriesData[msg.Chat.Id]["GroupId"] = user.SubGroupCode;
                    else
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Воспользуйтесь клавитурой ниже");
                        return;
                    }
                    await Bot.SendMessage(msg.Chat.Id, "Введите имя для новой категории", replyMarkup: new ReplyKeyboardRemove());
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForGroupIdCategory);
                    break;
                case UserState.WaitingForGroupIdCategory:
                    if (CategoriesData[msg.Chat.Id]["IsAutomatic"] != "")
                    {
                        var sch = new Table.Schedule("C:\\Users\\Пользователь\\queueManager\\Infrastructure\\Parser\\РасписаниеФИИТ2025осень.xlsx");

                        var cats = sch.GetSubjectsByGroup(CategoriesData[msg.Chat.Id]["GroupId"]);
                        if (!cats.Contains(msg.Text))
                        {
                            await Bot.SendMessage(msg.Chat.Id, "Введите название предмета из расписания");
                            return;
                        }
                    }
                    CategoriesData[msg.Chat.Id]["CategotyName"] = msg.Text;
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForGroupIdCategory);

                    var flag = false;
                    if (CategoriesData[msg.Chat.Id]["IsAutomatic"] != "")
                        flag = true;

                    var category = new WebApi.Controllers.BotGroupController.CategoryDto(
                        CategoriesData[msg.Chat.Id]["GroupId"],
                        flag,
                        CategoriesData[msg.Chat.Id]["CategotyName"]);
                    var response = await controllerUser.CreateCategory(category);


                    if (response)
                    {
                        CategoriesData.Remove(msg.Chat.Id);
                        await Bot.SendMessage(msg.Chat.Id, "Категория успешно создана", replyMarkup: new ReplyKeyboardRemove());
                        StateManager.SetState(msg.Chat.Id, UserState.None);
                    }
                    else
                    {
                        await Bot.SendMessage(msg.Chat.Id, $"Ошибка сохранения", replyMarkup: new ReplyKeyboardRemove());
                        StateManager.SetState(msg.Chat.Id, UserState.None);
                    }
                    StateManager.SetState(msg.Chat.Id, UserState.None);
                    break;
            }
        }
    }
}
