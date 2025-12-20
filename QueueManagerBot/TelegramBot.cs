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
        
        public TelegramBot(
            string token, 
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            httpClientFactory = httpClientFactory;
            configuration = configuration;
            bot = new TelegramBotClient(token);
            bot.OnMessage += OnMessage;
            bot.OnUpdate += OnUpdate;
            stateManager = new StateManager();
            apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:5001";
            notificationTimer = new Timer(async _ => 
            {
                await CheckAndSendNotificationsAsync();
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(30));
            
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

                new DeleteQueueCommand("/delete_queue", 
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

            };
            
            Console.WriteLine($"Бот инициализирован. API: {configuration["ApiBaseUrl"]}");
        }
        
        async Task OnMessage(Message msg, UpdateType type)
        {
            var isUserRegistered = await IsUserRegistered(msg.Chat.Id);
            if (!isUserRegistered && msg.Text != "/start")
                await bot.SendMessage(msg.Chat.Id, "Зарегистрируйтесь при помощи команды /start");

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
                if (query.Data.StartsWith("delete_queue_"))
                {
                    var eventIdString = query.Data.Replace("delete_queue_", "");
                    var response = await httpClient.PostAsJsonAsync(
                        $"{apiBaseUrl}/api/events/delete_queue", 
                        new { EventId = eventIdString }
                    );
                    
                    var chatId = query.Message.Chat.Id;
                    if (response.IsSuccessStatusCode)
                    {
                        await bot.SendMessage(chatId, "✅ Удалено!");
                        await bot.DeleteMessage(chatId, query.Message.MessageId);
                        stateManager.SetState(chatId, UserState.None);
                    }
                }

                if (query.Data.StartsWith("confirm_queue_from"))
                {
                    var parts = query.Data.Split('_');
                    
                    var fromIndex = Array.IndexOf(parts, "from");
                    var toIndex = Array.IndexOf(parts, "to");
                    
                    if (fromIndex != -1 && toIndex != -1 && toIndex > fromIndex + 1)
                    {
                        var userId = parts[fromIndex + 1];
                        var queueId = string.Join("_", parts.Skip(toIndex + 1));

                        var telegramId = long.Parse(userId);
                        var eventId = Guid.Parse(queueId);
                        
                        var participant = new WebApi.Controllers.BotEventController.ParticipationDto(
                            telegramId,
                            eventId,
                            Domain.Entities.UserPreference.NoPreference
                        );
                        await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/events/confirm", participant);
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
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] Проверяем очереди для уведомлений...");
                
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                
                var notificationResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/events/due-events-notification");
                
                if (notificationResponse.IsSuccessStatusCode)
                {
                    var notificationEvents = await notificationResponse.Content.ReadFromJsonAsync<List<WebApi.Controllers.BotEventController.BotEventDto>>();
                    
                    if (notificationEvents != null && notificationEvents.Any())
                    {
                        Console.WriteLine($"Найдено {notificationEvents.Count} очередей для уведомления");
                        
                        foreach (var eventDto in notificationEvents)
                        {
                            await SendNotificationsForEventAsync(eventDto);
                        }
                        
                        var eventIds = notificationEvents.Select(e => e.EventId).ToList();
                        await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/events/mark-notified", 
                            new { Ids = eventIds });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при проверке уведомлений: {ex.Message}");
            }
        }


        private async Task SendNotificationsForEventAsync(WebApi.Controllers.BotEventController.BotEventDto eventDto)
        {
            try
            {
                foreach (var telegramId in eventDto.TelegramId)
                {
                    var keyboard = new InlineKeyboardMarkup(new[]
                    {
                        new[]
                        {
                            InlineKeyboardButton.WithCallbackData(
                                "✅ Записаться",
                                $"confirm_queue_from_{eventDto.EventId}_to_{telegramId}"
                            )
                        }
                    });
                    
                    await bot.SendMessage(
                        telegramId,
                        $"📋 Уведомление о очереди!\n\n" +
                        $"Категория: {eventDto.Category}\n" +
                        $"Дата: {eventDto.OccurredOn:dd.MM.yyyy HH:mm}\n\n" +
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

        private async Task SendFormationNotificationAsync(WebApi.Controllers.BotEventController.BotEventDto eventDto)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var participantsInfo = new List<(long Id, string Username, string FullName)>();
                
                foreach (var telegramId in eventDto.TelegramId)
                {
                    try
                    {
                        var userResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/users/user-info?telegramId={telegramId}");
                        
                        if (userResponse.IsSuccessStatusCode)
                        {
                            var userInfo = await userResponse.Content.ReadFromJsonAsync<WebApi.Controllers.BotUserController.InfoUserDto>();
                            if (userInfo != null)
                            {
                                var displayName = !string.IsNullOrEmpty(userInfo.Username) 
                                    ? $"@{userInfo.Username}" 
                                    : userInfo.FullName;
                                
                                participantsInfo.Add((telegramId, displayName, userInfo.FullName));
                            }
                            else
                            {
                                participantsInfo.Add((telegramId, $"Пользователь #{telegramId}", "Неизвестно"));
                            }
                        }
                        else
                        {
                            participantsInfo.Add((telegramId, $"Пользователь #{telegramId}", "Неизвестно"));
                        }
                    }
                    catch
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
                    
                    await bot.SendMessage(
                        telegramId,
                        $"🏁 *Очередь сформирована!*\n\n" +
                        $"📌 *Категория:* {eventDto.Category}\n" +
                        $"📅 *Дата и время:* {eventDto.OccurredOn:dd.MM.yyyy HH:mm}\n" +
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
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при отправке уведомления о формировании: {ex.Message}");
            }
        }
        private async Task<bool> IsUserRegistered(long telegramId)
        {
            try
            {
                var httpClient = httpClientFactory.CreateClient("ApiClient");
                var userResponse = await httpClient.GetAsync($"{apiBaseUrl}/api/users/user-info?telegramId={telegramId}");
                
                return userResponse.IsSuccessStatusCode;
            }
            catch
            {
                return false;
            }
        }

    }
}

