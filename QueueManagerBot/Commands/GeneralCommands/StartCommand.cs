using Telegram.Bot;
using Telegram.Bot.Types;

namespace QueueManagerBot
{
    public class StartCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }

        public StartCommand(string name, TelegramBotClient tgBot, StateManager stateManager)
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
            // if (db.get(msg.Chat.Id) != null) 
            //      await Bot.SendMessage(msg.Chat, "Вы уже зарегистрированы, для получения списка команд введите /help")
            await Bot.SendMessage(msg.Chat, 
                "Добро пожаловать!\nДля регистрации в боте введите @fiitobot [Ваши Фамилия Имя] и нажмите на всплывающее окно\n\nПример: @fiitobot Иванов Иван");
            StateManager.SetState(msg.Chat.Id, UserState.WaitingForStudentData);
        }
    }
}
