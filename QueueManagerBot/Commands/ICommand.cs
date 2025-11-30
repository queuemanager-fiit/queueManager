using Telegram.Bot;
using Telegram.Bot.Types;

namespace QueueManagerBot
{
    public interface ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }
        public bool CanExecute(Message msg, UserState state);
        public Task Execute(Message msg);
    }
}