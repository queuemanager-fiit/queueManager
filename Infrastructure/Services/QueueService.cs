using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services;

public class QueueFormationService : BackgroundService
{
    private readonly IServiceProvider serviceProvider;
    private readonly TimeSpan checkInterval = TimeSpan.FromSeconds(60);

    public QueueFormationService(IServiceProvider serviceProvider)
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
                using (var scope = serviceProvider.CreateScope())
                {
                    var eventRepository = scope.ServiceProvider
                        .GetRequiredService<IEventRepository>();

                    var now = DateTimeOffset.UtcNow;

                    var pendingFormation = await eventRepository
                        .GetDueFormationAsync(now, stoppingToken);

                    foreach (var eventItem in pendingFormation)
                    {
                        if (!eventItem.IsFormed)
                        {
                            eventItem.FormQueue();
                            await eventRepository.UpdateAsync(eventItem, stoppingToken);
                        }
                    }

                    var expiredEvents = pendingFormation
                        .Where(e => e.DeletionTime <= now)
                        .ToList();

                    foreach (var eventItem in expiredEvents)
                    {
                        await eventRepository.DeleteAsync(eventItem, stoppingToken);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при обработке очередей: {ex.Message}");
            }

            await Task.Delay(checkInterval, stoppingToken);
        }
    }
}
