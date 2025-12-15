using Telegram.Bot;
using Telegram.Bot.Types;

namespace QueueManagerBot
{
    public class HelpCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }

        public HelpCommand(string name, TelegramBotClient tgBot, StateManager stateManager)
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
            await Bot.SendMessage(msg.Chat, 
                @"Queue Manager — ваш надёжный помощник в борьбе с хаосом живых очередей в чатах!

Список доступных команд:

/start - Начало работы с ботом
/info - Подробное описание бота и его возможностей
/help - Получить список команд
/get_queues - Получить полный список очередей
/create_queue - Создать очередь
/delete_queue - Удалить очередь
/update_queue - Обновить очередь

С помощью этого бота вы можете:
• Создавать группы и управлять ими
• Организовывать упорядоченные очереди на практики, сдачи заданий и другие события
• Заблаговременно записываться в очередь
• Указывать свои предпочтения (""хочу в начало"", ""хочу в конец"", ""рядом с другом"")
Больше не нужно постоянно мониторить чаты и бояться пропустить очередь! Queue Manager сделает процесс записи справедливым и удобным.");
        }
    }
}