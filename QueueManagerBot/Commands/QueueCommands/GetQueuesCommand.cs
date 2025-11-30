using Telegram.Bot;
using Telegram.Bot.Types;

namespace QueueManagerBot
{
    public class GetQueuesCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }

        public GetQueuesCommand(string name, TelegramBotClient tgBot, StateManager stateManager)
        {
            Name = name;
            Bot = tgBot;
            StateManager = stateManager;
            AllowedStates = new UserState[]
            {
                UserState.None,
            };
        }

        public bool CanExecute(Message msg, UserState state)
        {
            return msg.Text == Name && AllowedStates.Contains(state);
        }

        public async Task Execute(Message msg)
        {
            //queues = db.GetQueues(msg.Chat.Id);
            //queues.ForEach(queue => await Bot.SendMessage(msg.Chat.Id, queue.Name));
        }
    }
}
