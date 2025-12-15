using Telegram.Bot;
using Telegram.Bot.Types;

namespace QueueManagerBot
{
    public class InfoCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }

        public InfoCommand(string name, TelegramBotClient tgBot, StateManager stateManager)
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
                @"🤖 Queue Manager — умные очереди вместо хаоса в чатах

Проблема: Пока вы слущаете пару или спите, в чатах образуются стихийные очереди на сдачу практик. В них трудно разобраться, а попасть в начало — почти нереально.

Решение:
Бот создаёт честные и прозрачные очереди:

• 🗓️ Запись открывается заранее по расписанию
• ⚖️ Умный алгоритм выравнивает шансы всех участников
• 🎯 Учитывает ваши пожелания: «в начало», «в конец», «рядом с другом»
• 📊 Весь history очередей сохраняется

Итог: Чистые чаты без спама и справедливое распределение мест.");
        }
    }
}