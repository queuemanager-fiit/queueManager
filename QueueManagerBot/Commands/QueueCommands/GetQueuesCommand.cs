using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text;
using System.Text.Json;
using Telegram.Bot;
using Telegram.Bot.Types;
using WebApi.Controllers;

namespace QueueManagerBot
{
    public class GetQueuesCommand : ICommand
    {
        public string Name { get; }
        public TelegramBotClient Bot { get; }
        public UserState[] AllowedStates { get; }
        public StateManager StateManager { get; }
        private readonly HttpClient httpClient;
        private readonly string apiBaseUrl;

        public GetQueuesCommand(
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
                UserState.None,
            };

            httpClient = httpClientFactory.CreateClient("ApiClient");
            apiBaseUrl = configuration["ApiBaseUrl"] ?? "https://localhost:5001";
        }

        public bool CanExecute(Message msg, UserState state)
        {
            return msg.Text == Name && AllowedStates.Contains(state);
        }

        public async Task Execute(Message msg)
        {
            var controllerUser = new ControllerUser(httpClient, apiBaseUrl);
            var events = await controllerUser.GetQueueList(msg.Chat.Id);
            var participantsList = new StringBuilder();


            if (events != null)
            {       
                if (events?.Any() == true)
                {
                    foreach (var e in events)
                    {
                        var counter = 0;
                        foreach (var participantId in e.TelegramId)
                        {
                            var userInfo = await controllerUser.GetUser(participantId);
                            var position = counter + 1;
                            var fullName = userInfo.FullName;
                            participantsList.AppendLine($"{position}. {fullName}");
                        }
                    
                        await Bot.SendMessage(
                            msg.Chat.Id,
                            $"🎯 Событие: {e.Category}\n\n" +
                            $"⏰ Время: {e.OccurredOn:g}\n");
                    }
                }
                else
                {
                    await Bot.SendMessage(msg.Chat.Id, "Нет активных событий");
                }
            }
            else
            {
                await Bot.SendMessage(msg.Chat.Id, $"Ошибка при получении данных");
            }
            
        }
    }
}