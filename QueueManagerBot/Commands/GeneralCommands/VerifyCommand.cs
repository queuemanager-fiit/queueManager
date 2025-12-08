using Telegram.Bot;
using Telegram.Bot.Types;
using System.Text.RegularExpressions;
using System.Data;
using WebApi.Controllers;
using Microsoft.Extensions.Configuration;
using System.Net.Http.Json;

namespace QueueManagerBot
{
    public class VerifyCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }
        private readonly HttpClient httpClient;
        private readonly string apiBaseUrl;

        public VerifyCommand(
            string name, 
            TelegramBotClient tgBot, 
            StateManager stateManager,
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration)
        {
            Name = name;
            Bot = tgBot;
            StateManager = stateManager;
            AllowedStates = new UserState[]
            {
                UserState.WaitingForStudentData,
            };
            
            httpClient = httpClientFactory.CreateClient("ApiClient");
            apiBaseUrl = configuration["ApiBaseUrl"] ?? "http://localhost:5134";
        }

        public bool CanExecute(Message msg, UserState state)
        {
            return AllowedStates.Contains(state);
        }

        public async Task Execute(Message msg)
        {
            try
            {
                var data = GetStudentData(msg.Text, msg.Chat.Id);
                
                if (data != null && 
                    data.Username == "@" + msg.Chat.Username && 
                    msg.ViaBot != null && 
                    msg.ViaBot!.Username == "fiitobot")
                {

                    var response = await httpClient.PostAsJsonAsync($"{apiBaseUrl}/api/users/update-userinfo", data);                     
                    
                    if (response.IsSuccessStatusCode)
                    {
                        await Bot.SendMessage(msg.Chat.Id, "Ваши данные приняты и сохранены в системе");
                        await Bot.SendMessage(msg.Chat.Id, "ℹЕсли хотите узнать список команд, введите /help");
                        await Bot.SendMessage("699010737", $"Новый пользователь @{msg.Chat.Username}");
                        StateManager.SetState(msg.Chat.Id, UserState.None);
                    }
                    else
                    {
                        var errorContent = await response.Content.ReadAsStringAsync();
                        await Bot.SendMessage(msg.Chat.Id, $"Ошибка сохранения: {response.StatusCode}\n{errorContent}");
                    }
                }
                else
                {
                    await Bot.SendMessage(msg.Chat.Id, "Вы ввели не свои данные или данные неверны");
                }
            }
            catch (HttpRequestException ex)
            {
                await Bot.SendMessage(msg.Chat.Id, "Ошибка подключения к серверу. Попробуйте позже.");
            }
            catch (Exception ex)
            {
                await Bot.SendMessage(msg.Chat.Id, "Произошла непредвиденная ошибка");
                Console.WriteLine(ex.Message);
            }
        }

        private static WebApi.Controllers.BotUserController.BotUserDto? GetStudentData(string input, long tgID)
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
            Console.WriteLine($"{fullName}, {group}, {username}");
            return new WebApi.Controllers.BotUserController.BotUserDto(fullName, username, group, tgID);
        }
    }
}