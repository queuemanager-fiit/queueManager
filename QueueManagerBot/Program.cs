﻿using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueueManagerBot;

namespace QueueManagerBot
{
    class Program
    {
        static void Main()
        {
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ApiBaseUrl"] = "http://localhost:5134",
                    ["TelegramBotToken"] = "8239008677:AAEvfak7nVlO4zCWCgUQvbsqXHAdBsf7blA"
                })
                .Build();
            
            var services = new ServiceCollection();
            services.AddHttpClient();
            var serviceProvider = services.BuildServiceProvider();
            
            var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
            
            var bot = new TelegramBot(
                configuration["TelegramBotToken"],
                httpClientFactory,
                configuration);
            
            Console.ReadLine();
        }
    }
}