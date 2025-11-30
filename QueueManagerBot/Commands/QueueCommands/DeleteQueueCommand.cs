using Telegram.Bot;
using Telegram.Bot.Types;

namespace QueueManagerBot
{
    public class DeleteQueueCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }

        public DeleteQueueCommand(string name, TelegramBotClient tgBot, StateManager stateManager)
        {
            Name = name;
            Bot = tgBot;
            StateManager = stateManager;
            AllowedStates = new UserState[]
            {
                UserState.None,
                UserState.WaitingForQueueNameToDelete,
            };
        }

        public bool CanExecute(Message msg, UserState state)
        {
            return msg.Text == Name && AllowedStates.Contains(state);
        }

        public async Task Execute(Message msg)
        {
            switch (StateManager.GetState(msg.Chat.Id))
            {
                case UserState.None:
                    await Bot.SendMessage(msg.Chat.Id, "Выберете очередь для удаления");
                    // db.GetAdminQueues(msg.Chat.Id)
                    // клава
                    StateManager.SetState(msg.Chat.Id, UserState.WaitingForQueueNameToDelete);
                    break;
                case UserState.WaitingForQueueNameToDelete:
                    // db.DeleteQueue(id)
                    await Bot.SendMessage(msg.Chat.Id, "Очередь успешно удалена");
                    StateManager.SetState(msg.Chat.Id, UserState.None);
                    break;
            }
        }
    }
}
