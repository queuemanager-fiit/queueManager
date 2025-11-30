using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;
using System.Data;

namespace QueueManagerBot
{
    public class VerifyCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }

        public VerifyCommand(string name, TelegramBotClient tgBot, StateManager stateManager)
        {
            Name = name;
            Bot = tgBot;
            StateManager = stateManager;
            AllowedStates = new UserState[]
            {
                UserState.WaitingForStudentData,
            };
        }

        public bool CanExecute(Message msg, UserState state)
        {
            return AllowedStates.Contains(state);
        }

        public async Task Execute(Message msg)
        {
            var data = GetStudentData(msg.Text);
            if (data != null && data.username == "@" + msg.Chat.Username && msg.ViaBot != null && msg.ViaBot!.Username == "fiitobot")
            {
                data.telegramID = msg.Chat.Id;
                // db.Add(data)
                await Bot.SendMessage(msg.Chat.Id, "Ваши данные приняты");
                await Bot.SendMessage(msg.Chat.Id, "Если хотите узнать список команд, введите /help");
                await Bot.SendMessage("699010737", $"Новый пользователь @{msg.Chat.Username}");
                StateManager.SetState(msg.Chat.Username!, UserState.None);
            }
            else
            {
                await Bot.SendMessage(msg.Chat.Id, "Ваши ввели не свои данные");
            }
        }

        private static StudentData? GetStudentData(string input)
        {
            var namePattern = @"^([А-ЯЁ][а-яё]+)\s+([А-ЯЁ][а-яё]+)";
            var nameMatch = Regex.Match(input, namePattern, RegexOptions.Multiline);
            var fullName = nameMatch.Success ? $"{nameMatch.Groups[1].Value} {nameMatch.Groups[2].Value}" : null;

            var groupPattern = @"(ФТ-[0-9]+-[0-9]+)";
            var groupMatch = Regex.Match(input, groupPattern);
            var group = groupMatch.Success ? groupMatch.Groups[1].Value : null;

            var usernamePattern = @"@[\w\d_]+";
            var usernameMatch = Regex.Match(input, usernamePattern);
            var username = usernameMatch.Success ? usernameMatch.Value : null;

            if (group == null || fullName == null || username == null)
                return null;

            return new StudentData(fullName, group, username);
        }
    }
}