using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace QueueManagerBot
{
    class TelegramBot
    {
        private StateManager stateManager;
        private TelegramBotClient bot;
        private List<ICommand> Commands;
        private readonly IHttpClientFactory httpClientFactory;
        private readonly IConfiguration configuration; 
        
        public TelegramBot(
            string token, 
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            httpClientFactory = httpClientFactory;
            configuration = configuration;
            bot = new TelegramBotClient(token);
            bot.OnMessage += OnMessage;
            stateManager = new StateManager();
            
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
            await bot.SendMessage(msg.Chat.Id, $"{stateManager.GetState(msg.Chat.Id)}");
            if (command != null)
                await command.Execute(msg);
        }
    }
}