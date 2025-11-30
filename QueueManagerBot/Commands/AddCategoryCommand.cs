using System.Data.Common;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.ReplyMarkups;

namespace QueueManagerBot
{
    public class AddCategoryCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }
        public Dictionary<long, Dictionary<string, string>> QueuesData { get; } = new Dictionary<long, Dictionary<string, string>>();

        public AddCategoryCommand(string name, TelegramBotClient tgBot, StateManager stateManager)
        {
            Name = name;
            Bot = tgBot;
            StateManager = stateManager;
            AllowedStates = new UserState[]
            {
                UserState.None,
                UserState.WaitingForNewCategoryName
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
                    await Bot.SendMessage(msg.Chat.Id, "Введите имя для новой категории");
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForNewCategoryName);
                    break;
                case UserState.WaitingForNewCategoryName:
                    // db.AddCategory(msg.Text)
                    StateManager.SetState(msg.Chat.Id, UserState.None);
                    await Bot.SendMessage(msg.Chat.Id, "Категория успешно добавлена");
                    break;

            }
        }
    }
}