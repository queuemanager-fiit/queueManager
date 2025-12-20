using Application.Interfaces;
using Domain.Entities;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services;

public class QueueFormationService : IHostedService, IDisposable
{
    private Timer? timer;
    private readonly IEventRepository eventRepository;
    private readonly TimeSpan checkInterval = TimeSpan.FromSeconds(60);

    public QueueFormationService(IEventRepository eventRepository)
    {
        this.eventRepository = eventRepository ?? throw new ArgumentNullException(nameof(eventRepository));
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        timer = new Timer(
            CheckAndProcessQueuesAsync,
            null,
            TimeSpan.Zero,
            checkInterval
        );

        return Task.CompletedTask;
    }

    private async void CheckAndProcessQueuesAsync(object? state)
    {
        try
        {
            var now = DateTimeOffset.UtcNow;

            var pendingFormation = await eventRepository.GetDueFormationAsync(now, CancellationToken.None);

            foreach (var eventItem in pendingFormation)
            {
                if (!eventItem.IsFormed)
                {
                    eventItem.FormQueue();
                    await eventRepository.UpdateAsync(eventItem, CancellationToken.None);
                }
            }

            var expiredEvents = pendingFormation
                .Where(e => e.DeletionTime <= now)
                .ToList();

            foreach (var eventItem in expiredEvents)
            {
                await eventRepository.DeleteAsync(eventItem, CancellationToken.None);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"������ ��� ��������� ��������: {ex.Message}");
        }
    }

    public Task StopAsync(CancellationToken cancellationToken)
    {
        timer?.Change(Timeout.Infinite, 0);
        return Task.CompletedTask;
    }

    public void Dispose()
    {
        timer?.Dispose();
    }
}
