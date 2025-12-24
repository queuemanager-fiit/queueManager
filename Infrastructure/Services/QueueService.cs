using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Repositories;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Infrastructure.Services
{
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
                        var eventRepository = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                        var groupRepository = scope.ServiceProvider.GetRequiredService<IGroupRepository>();
                        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
                        var eventCategoryRepository = scope.ServiceProvider.GetRequiredService<IEventCategoryRepository>();
                        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

                        var now = DateTimeOffset.UtcNow;

                        var pendingFormation = await eventRepository
                            .GetDueFormationAsync(now, stoppingToken);

                        foreach (var eventItem in pendingFormation)
                        {
                            if (!eventItem.IsFormed)
                            {
                                await FormQueueAsync(eventItem, userRepository, eventCategoryRepository, stoppingToken);
                                await eventRepository.UpdateAsync(eventItem, stoppingToken);
                            }
                        }

                        var expiredEvents = pendingFormation
                            .Where(e => e.DeletionTime <= now)
                            .ToList();

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

                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при обработке очередей: {ex.Message}");
                }

                await Task.Delay(checkInterval, stoppingToken);
            }
        }

        private async Task FormQueueAsync(
            Event eventItem,
            IUserRepository userRepository,
            IEventCategoryRepository eventCategoryRepository,
            CancellationToken ct)
        {
            if (eventItem.IsFormed) return;

            var category = await eventCategoryRepository.GetByIdAsync(eventItem.CategoryId, ct);
            if (category == null)
            {
                Console.WriteLine($"Категория не найдена для события {eventItem.Id}. Пропускаем формирование очереди.");
                return;
            }

            var unfinishedIds = category.UnfinishedUsersTelegramIds;
            var participantIds = eventItem.ParticipantsTelegramIds;
            var preferences = eventItem.Preferences;

            var participantPreferenceList = new List<(long TelegramId, UserPreference Preference)>();
            for (int i = 0; i < participantIds.Count; i++)
            {
                participantPreferenceList.Add((participantIds[i], preferences[i]));
            }

            var users = await Task.WhenAll(
            (
                from id in participantIds
                select userRepository.GetByTelegramIdAsync(id, ct)
            ));

            var userDict = users
                .Where(u => u != null)
                .ToDictionary(u => u.TelegramId, u => u);

            var sortedParticipantIds = participantPreferenceList
                .OrderBy(p => p.Preference)
                .ThenBy(p => userDict.ContainsKey(p.TelegramId)
                    ? userDict[p.TelegramId].AveragePosition
                    : 0.0)
                .Select(p => p.TelegramId)
                .ToList();

            var finalQueue = new List<long>();

            foreach (var unfinishedId in unfinishedIds)
            {
                int index = participantIds.IndexOf(unfinishedId);
                if (index != -1 && preferences[index] != UserPreference.End)
                {
                    finalQueue.Add(unfinishedId);
                    sortedParticipantIds.Remove(unfinishedId);
                }
            }

            finalQueue.AddRange(sortedParticipantIds);

            eventItem.ParticipantsTelegramIds.Clear();
            eventItem.ParticipantsTelegramIds.AddRange(finalQueue);
            eventItem.IsFormed = true;

            for (int i = 0; i < eventItem.ParticipantsTelegramIds.Count; i++)
            {
                var userId = eventItem.ParticipantsTelegramIds[i];
                if (userDict.ContainsKey(userId))
                {
                    userDict[userId].UpdateAveragePosition(i + 1);
                    await userRepository.UpdateAsync(userDict[userId], ct);
                }
            }

            category.UnfinishedUsersTelegramIds.Clear();
            await eventCategoryRepository.UpdateAsync(category, ct);
        }
    }
}
