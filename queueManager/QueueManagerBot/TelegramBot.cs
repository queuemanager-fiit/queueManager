using System.ComponentModel;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using System.Net.Http.Json;

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
            
            Commands = new List<ICommand>()
            {
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
                    configuration)
            };
            
            Console.WriteLine($"Бот инициализирован. API: {configuration["ApiBaseUrl"]}");
        }
        
        async Task OnMessage(Message msg, UpdateType type)
        {
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
                
                if (query.Data.StartsWith("delete_queue_"))
                {
                    var eventIdString = query.Data.Replace("delete_queue_", "");
                    var httpClient = httpClientFactory.CreateClient("ApiClient");
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
            }
        }
    }
}