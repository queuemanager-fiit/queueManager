using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace QueueManagerBot
{
    public class CreateQueueCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }
        public Dictionary<long, Dictionary<string, string>> QueuesData { get; } = new Dictionary<long, Dictionary<string, string>>();

        public CreateQueueCommand(string name, TelegramBotClient tgBot, StateManager stateManager)
        {
            Name = name;
            Bot = tgBot;
            StateManager = stateManager;
            AllowedStates = new UserState[]
            {
                UserState.None,
                UserState.WaitingForQueueName,
                UserState.WaitingForQueueCategory
            };
        }

        public bool CanExecute(Message msg, UserState state)
        {
            // var isAdmin = db.IsAdmin(msg.Chat.Id);
            return (msg.Text == Name && state == UserState.None) || (state != UserState.None && AllowedStates.Contains(state));
        }

        public async Task Execute(Message msg)
        {
            switch (StateManager.GetState(msg.Chat.Id))
            {
                case UserState.None:
                    await Bot.SendMessage(msg.Chat.Id, "Введите название очереди");
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForQueueName);
                    break;  
                case UserState.WaitingForQueueName:
                    // cats = db.GetCategories()
                    await Bot.SendMessage(msg.Chat.Id, "Введите название категории", replyMarkup: new InlineKeyboardButton[][]
    {
        [("1.1", "11"), ("1.2", "12")], // two buttons on first row
        [("2.1", "21"), ("2.2", "22")]  // two buttons on second row
    });
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForQueueCategory);
                    QueuesData[msg.Chat.Id]["QueueName"] = msg.Text!;
                    break;
                case UserState.WaitingForQueueCategory:
                    await Bot.SendMessage(msg.Chat.Id, "Очередь успешно создана");
                    StateManager.SetState(msg.Chat.Id, UserState.None);
                    QueuesData[msg.Chat.Id]["QueueCategory"] = msg.Text!;
                    // db.Add()
                    QueuesData.Remove(msg.Chat.Id);
                    break;
            }
        }
    }
}