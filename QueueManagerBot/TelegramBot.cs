using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;

namespace QueueManagerBot
{

    class TelegramBot
    {
        private StateManager stateManager;
        private static TelegramBotClient bot;
        private static List<ICommand> Commands;
        public TelegramBot(string token)
        {
            bot = new TelegramBotClient(token);
            bot.OnMessage += OnMessage;
            stateManager = new StateManager();
            
            Commands = new List<ICommand>()
            {
                new StartCommand("/start", bot, stateManager),
                new HelpCommand("/help", bot, stateManager),
                new InfoCommand("/info", bot, stateManager),
                new CreateQueueCommand("/create_queue", bot, stateManager),
                new DeleteQueueCommand("/delete_queue", bot, stateManager),
                new GetQueuesCommand("/get_queues", bot, stateManager),
                new VerifyCommand("", bot, stateManager)
            };
        }

        async Task OnMessage(Message msg, UpdateType type)
        {
            var command = Commands
                .FirstOrDefault(command => command.CanExecute(msg, stateManager.GetState(msg.Chat.Id)));

            if (command != null)
                await command.Execute(msg);
        }
    }

    class StudentData
    {
        public readonly string fullname;
        public readonly string group;
        public readonly string username;
        public long telegramID;

        public StudentData(string fullname, string group, string username)
        {
            this.fullname = fullname;
            this.group = group;
            this.username = username;
        }
    }
}
