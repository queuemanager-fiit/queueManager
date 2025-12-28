using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
    public class ExpiredEventService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;

        private static readonly TimeSpan interval = TimeSpan.FromHours(6);

        public ExpiredEventService(IServiceProvider serviceProvider)
        {
            this.serviceProvider = serviceProvider ??
                throw new ArgumentNullException(nameof(serviceProvider));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformCleanup(stoppingToken);
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке очередей: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task PerformCleanup(CancellationToken stoppingToken)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                var groupRepository = scope.ServiceProvider.GetRequiredService<IGroupRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                var now = DateTimeOffset.UtcNow;
                var expiredEvents = await eventRepository
                    .GetExpiredEventsAsync(now, stoppingToken);

                foreach (var eventItem in expiredEvents)
                {
                    var group = await groupRepository.GetByCodeAsync(eventItem.GroupCode, stoppingToken);
                    if (group == null)
                    {
                        Console.WriteLine($"Группа {eventItem.GroupCode} не найдена для события {eventItem.Id}");
                        continue;
                    }

                    group.RemoveEvent(eventItem.Id);
                    await groupRepository.UpdateAsync(group, stoppingToken);
                    await eventRepository.DeleteAsync(eventItem, stoppingToken);
                }

                await unitOfWork.SaveChangesAsync(stoppingToken);
                Console.WriteLine($"[{DateTimeOffset.UtcNow}] Очищено {expiredEvents.Count} устаревших событий.");
            }
        }
    }
}
