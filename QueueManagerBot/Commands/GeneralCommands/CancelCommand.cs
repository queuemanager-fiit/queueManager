using Telegram.Bot;
using Telegram.Bot.Types;

namespace QueueManagerBot
{
    public class CancelCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }

        public CancelCommand(string name, TelegramBotClient tgBot, StateManager stateManager)
        {
            Name = name;
            Bot = tgBot;
            StateManager = stateManager;
            AllowedStates = new UserState[]
            {
                UserState.None,
                UserState.WaitingForStudentData,
                UserState.WaitingForQueueName,
                UserState.WaitingForQueueCategory,
                UserState.WaitingForQueueNameToDelete,
                UserState.WaitingForNewCategoryName,
                UserState.WaitingForQueueDate,
                UserState.WaitingForGroupId,
                UserState.WaitingForGroupIdCategory,
            };

        }

        public bool CanExecute(Message msg, UserState state)
        {
            return msg.Text == Name;
        }

        public async Task Execute(Message msg)
        {
            StateManager.SetState(msg.Chat.Id, UserState.None);
            await Bot.SendMessage(msg.Chat, "Команда отменена");
        }
    }
}
