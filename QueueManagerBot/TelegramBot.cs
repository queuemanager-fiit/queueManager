using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Net.Http.Json;
using Telegram.Bot.Types.ReplyMarkups;
using System.Text;

namespace QueueManagerBot
{
    class TelegramBot
    {
        private StateManager stateManager;
        private TelegramBotClient bot;
        private List<ICommand> Commands;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration;
        private readonly string apiBaseUrl;
        private readonly Timer notificationTimer;
        private readonly Timer formationTimer;


        public TelegramBot(
            string token,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            this.httpClientFactory = httpClientFactory;
            this.configuration = configuration;
            bot = new TelegramBotClient(token);
            bot.OnMessage += OnMessage;
            bot.OnUpdate += OnUpdate;
            stateManager = new StateManager();
            apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:5001";
            
            notificationTimer = new Timer(async _ =>
            {
                await CheckAndSendNotificationsAsync();
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            
            formationTimer = new Timer(async _ =>
            {
                await CheckAndSendFormationNotificationsAsync();
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1));

            Commands = new List<ICommand>()
            {
                new CancelCommand(
                    "/cancel",
                    bot,
                    stateManager
                ),

                new StartCommand(
                    "/start",
                    bot,
                    stateManager,
                    httpClientFactory,
                    configuration),

                new HelpCommand("/help",
                    bot,
                    stateManager),

                new InfoCommand("/info",
                    bot,
                    stateManager),

                new CreateQueueCommand("/create_queue",
                    bot,
                    stateManager,
                    httpClientFactory,
                    configuration),

                new GetQueuesCommand(
                    "/get_queues",
                    bot,
                    stateManager,
                    httpClientFactory,
                    configuration),

                new VerifyCommand("",
                    bot,
                    stateManager,
                    httpClientFactory,
                    configuration),

                new AddCategoryCommand(
                    "/create_category",
                    bot,
                    stateManager,
                    httpClientFactory,
                    configuration),
                new DeleteCategoryCommand(
                    "/delete_category",
                    bot,
                    stateManager,
                    httpClientFactory,
                    configuration
                )

            };

            Console.WriteLine($"Бот инициализирован. API: {configuration["ApiBaseUrl"]}");
        }

        async Task OnMessage(Message msg, UpdateType type)
        {
            var isUserRegistered = await IsUserRegistered(msg.Chat.Id);
            Console.WriteLine(isUserRegistered);
            if (!isUserRegistered && msg.Text != "/start" && stateManager.GetState(msg.Chat.Id) != UserState.WaitingForStudentData)
            {
                await bot.SendMessage(msg.Chat.Id, "Зарегистрируйтесь при помощи команды /start");
                return;
            }
            var command = Commands
                .FirstOrDefault(command => command.CanExecute(msg, stateManager.GetState(msg.Chat.Id)));
            if (command != null)
                await command.Execute(msg);
        }


        async Task OnUpdate(Update update)
        {
            if (update is { CallbackQuery: { } query })
            {
                await bot.AnswerCallbackQuery(query.Id);
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var controllerUser = new ControllerUser(httpClient, apiBaseUrl);
                var user = await controllerUser.GetUser(query.Message.Chat.Id);

                await bot.EditMessageReplyMarkup(
                    chatId: update.CallbackQuery.Message.Chat.Id,
                    messageId: update.CallbackQuery.Message.MessageId,
                    replyMarkup: null
                );

                if (query.Data.StartsWith("delete_category_"))
                {
                    Console.WriteLine("deledte");
                    var catNameString = query.Data.Replace("delete_category_", "");
                    var catDeletionSubGroup = new WebApi.Controllers.BotGroupController.DeletionDto(user.SubGroupCode, catNameString);
                    var catDeletionGroup = new WebApi.Controllers.BotGroupController.DeletionDto(user.GroupCode, catNameString);
                    var response1 = await controllerUser.DeleteCategory(catDeletionSubGroup);
                    var response2 = await controllerUser.DeleteCategory(catDeletionGroup);
                    var chatId = query.Message.Chat.Id;
                    if (response1 || response2)
                    {
                        await bot.SendMessage(chatId, "✅ Удалено!");
                        await bot.DeleteMessage(chatId, query.Message.MessageId);
                        stateManager.SetState(chatId, UserState.None);
                    }
                }

                if (query.Data.StartsWith("from"))
                {
                    var parts = query.Data.Split('_');

                    var fromIndex = Array.IndexOf(parts, "from");
                    var toIndex = Array.IndexOf(parts, "to");



                    if (fromIndex != -1 && toIndex != -1 && toIndex > fromIndex + 1)
                    {
                        var queueId = parts[fromIndex + 1];
                        var userId = parts[toIndex + 1];

                        var hasEnd = query.Data.Contains("_end");
                        var hasStart = query.Data.Contains("_start");

                        var telegramId = long.Parse(userId);
                        var eventId = Guid.Parse(queueId);

                        var pref = Domain.Entities.UserPreference.NoPreference;

                        if (hasStart)
                            pref = Domain.Entities.UserPreference.Start;
                        else
                            pref = Domain.Entities.UserPreference.End;

                        var participant = new WebApi.Controllers.BotEventController.ParticipationDto(
                            telegramId,
                            eventId,
                            pref
                        );
                        await controllerUser.ConfirmQueue(participant);
                    }
                }

                if (query.Data.StartsWith("select_category_"))
                {
                    var createQueueCommand = Commands.OfType<CreateQueueCommand>().FirstOrDefault();
                    await createQueueCommand.HandleCategoryCallback(query.Data, query.Message.Chat.Id);
                }
            }
        }


        private async Task CheckAndSendNotificationsAsync()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Проверяем очереди для уведомлений...");

            var httpClient = httpClientFactory.CreateClient("ApiClient");
            var controllerUser = new ControllerUser(httpClient, apiBaseUrl);
            var notificationEvents = await controllerUser.DueEventsNotification();
            if (notificationEvents != null)
            {
                if (notificationEvents.Any())
                {
                    Console.WriteLine($"Найдено {notificationEvents.Count} очередей для уведомления");

                    foreach (var eventDto in notificationEvents)
                    {
                        await SendNotificationsForEventAsync(eventDto);
                    }

                    var eventIds = notificationEvents.Select(e => e.EventId).ToList();
                    await controllerUser.MarkNotified(eventIds);
                }
            }
        }


        private async Task SendNotificationsForEventAsync(WebApi.Controllers.BotEventController.BotEventDto eventDto)
        {
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            var controllerUser = new ControllerUser(httpClient, apiBaseUrl);

            try
            {
                var tgIds = await controllerUser.GetGroupUsers(eventDto.GroupCode);


                foreach (var telegramId in tgIds)
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "Хочу в начало",
                                $"from_{eventDto.EventId:N}_to_{telegramId}_start"),
                            InlineKeyboardButton.WithCallbackData(
                                "Хочу в конец",
                                $"from_{eventDto.EventId:N}_to_{telegramId}_end"
                            )
                        }
                    });
                    var localTime = eventDto.OccurredOn.AddHours(5);
                    await bot.SendMessage(
                        telegramId,
                        $"📋 Уведомление о очереди!\n\n" +
                        $"Категория: {eventDto.Category}\n" +
                        $"Дата: {localTime:dd.MM.yyyy HH:mm}\n\n" +
                        $"Нажмите кнопку, чтобы записаться:",
                        replyMarkup: keyboard
                    );

                    Console.WriteLine($"Уведомление отправлено пользователю {telegramId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке уведомлений для события {eventDto.EventId}: {ex.Message}");
            }
        }



        private async Task CheckAndSendFormationNotificationsAsync()
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Проверяем сформированные очереди...");

            var httpClient = httpClientFactory.CreateClient("ApiClient");
            var controllerUser = new ControllerUser(httpClient, apiBaseUrl);
            
            try
            {
                var formedEvents = await controllerUser.DueEventsFormation();
                
                if (formedEvents == null || !formedEvents.Any())
                {
                    return;
                }
                
                foreach (var eventDto in formedEvents)
                {
                    if (eventDto.TelegramId == null || eventDto.TelegramId.Length == 0)
                    {
                        Console.WriteLine($"Очередь {eventDto.EventId} пустая. Пропускаем.");
                        continue;
                    }
                    
                    var participantsInfo = new List<(long Id, string Username, string FullName)>();
                    
                    foreach (var telegramId in eventDto.TelegramId)
                    {
                        var userInfo = await controllerUser.GetUser(telegramId);
                        if (userInfo != null)
                        {
                            var displayName = !string.IsNullOrEmpty(userInfo.Username)
                                ? $"{userInfo.Username}"
                                : userInfo.FullName;
                            participantsInfo.Add((telegramId, displayName, userInfo.FullName));
                        }
                        else
                        {
                            participantsInfo.Add((telegramId, $"Пользователь #{telegramId}", "Неизвестно"));
                        }
                    }

                    var participantsList = new StringBuilder();
                    participantsList.AppendLine("📋 *Список участников очереди:*\n");

                    for (int i = 0; i < participantsInfo.Count; i++)
                    {
                        var position = i + 1;
                        var (id, username, fullName) = participantsInfo[i];
                        participantsList.AppendLine($"{position}. {username}");
                    }

                    foreach (var telegramId in eventDto.TelegramId)
                    {
                        var userInfo = participantsInfo.FirstOrDefault(p => p.Id == telegramId);
                        var userPosition = participantsInfo.FindIndex(p => p.Id == telegramId) + 1;
                        var displayName = userInfo.Username ?? $"Пользователь #{telegramId}";
                        var localTime = eventDto.OccurredOn.AddHours(5);
                        await bot.SendMessage(
                            telegramId,
                            $"🏁 *Очередь сформирована!*\n\n" +
                            $"📌 *Категория:* {eventDto.Category}\n" +
                            $"📅 *Дата и время:* {localTime:dd.MM.yyyy HH:mm}\n" +
                            $"👥 *Количество участников:* {eventDto.TelegramId.Length}\n" +
                            $"📍 *Ваша позиция:* {userPosition}\n" +
                            $"👤 *Ваше имя:* {displayName}\n\n" +
                            participantsList.ToString() +
                            $"\n_Не опаздывайте!_ ⏰",
                            parseMode: ParseMode.Markdown
                        );

                        Console.WriteLine($"Уведомление о формировании отправлено пользователю {displayName} (ID: {telegramId})");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка: {ex.Message}");
            }
        }

        private async Task<bool> IsUserRegistered(long telegramId)
        {
            var httpClient = httpClientFactory.CreateClient("ApiClient");
            var controllerUser = new ControllerUser(httpClient, apiBaseUrl);
            var user = await controllerUser.GetUser(telegramId);
            if (user != null)
                return true;
            return false;
        }
    }
}
