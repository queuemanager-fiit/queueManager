using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using QueueManagerBot;

namespace QueueManagerBot
{
    class Program
    {
        static async Task Main()
        {
            EnvLoader.Load();
            var token = Environment.GetEnvironmentVariable("TELEGRAM_BOT_TOKEN");
            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string>
                {
                    ["ApiBaseUrl"] = "http://localhost:5134",
                    ["TelegramBotToken"] = token
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
            
            await Task.Delay(Timeout.Infinite);
        }
    }

    public static class EnvLoader
    {
        public static void Load(string filePath = ".env")
        {
            if (!File.Exists(filePath))
                return;
            
            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Split('=', 2);
                if (parts.Length != 2) continue;
                
                Environment.SetEnvironmentVariable(parts[0].Trim(), parts[1].Trim());
            }
        }
    }
}