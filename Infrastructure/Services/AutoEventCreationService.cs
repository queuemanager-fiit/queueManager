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
using Table;

namespace Infrastructure.Services
{
    public class AutoEventCreationService : BackgroundService
    {
        private readonly IServiceProvider serviceProvider;
        private readonly TimeSpan interval = TimeSpan.FromDays(1);

        private readonly Schedule schedule;

        public AutoEventCreationService(
            IServiceProvider serviceProvider,
            Schedule schedule)
        {
            this.serviceProvider = serviceProvider ??
                throw new ArgumentNullException(nameof(serviceProvider));
            this.schedule = schedule ?? throw new ArgumentNullException(nameof(schedule));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PerformAutoCreation(stoppingToken);
                    await Task.Delay(interval, stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при автоматическом создании событий: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }
        }

        private async Task PerformAutoCreation(CancellationToken ct)
        {
            using (var scope = serviceProvider.CreateScope())
            {
                var categoryRepo = scope.ServiceProvider.GetRequiredService<IEventCategoryRepository>();
                var groupRepo = scope.ServiceProvider.GetRequiredService<IGroupRepository>();
                var eventRepo = scope.ServiceProvider.GetRequiredService<IEventRepository>();
                var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();
                var autoCategories = await categoryRepo.GetAutoCreateCategoriesAsync(ct);

                if (!autoCategories.Any())
                    return;

                var today = DateTime.Today;
                var allGroups = schedule.CollectGroupInfo(today);
                var computedGroups = allGroups.ExtractGroupData();

                var now = DateTimeOffset.UtcNow;

                foreach (var category in autoCategories)
                {
                    var group = await groupRepo.GetByCodeAsync(category.GroupCode, ct);
                    if (group == null)
                    {
                        Console.WriteLine($"Группа {category.GroupCode} не найдена для категории {category.Id}");
                        continue;
                    }

                    int dashCount = category.GroupCode.Count(c => c == '-');
                    GroupInfo groupInfo = null;

                    if (dashCount == 1)
                    {
                        groupInfo = computedGroups.FirstOrDefault(g => g.Name == category.GroupCode);
                    }
                    else
                    {
                        groupInfo = allGroups.FirstOrDefault(g => g.Name == category.GroupCode);
                    }

                    if (groupInfo == null)
                    {
                        Console.WriteLine($"Расписание для {category.GroupCode} не найдено");
                        continue;
                    }

                    if (!groupInfo.Lessons.TryGetValue(today.DayOfWeek, out var lessons))
                        continue;

                    foreach (var lesson in lessons)
                    {
                        if (string.Equals(lesson.Name, category.SubjectName, StringComparison.OrdinalIgnoreCase))
                        {
                            var eventItem = new Event(
                                category.Id,
                                new DateTimeOffset(lesson.DateTime, TimeSpan.FromHours(5)),
                                category.GroupCode
                            );

                            await eventRepo.AddAsync(eventItem, ct);

                            group.AddEvent(eventItem.Id);
                            await groupRepo.UpdateAsync(group, ct);
                        }
                    }
                }

                await unitOfWork.SaveChangesAsync(ct);
                Console.WriteLine($"[{DateTimeOffset.UtcNow}] Автоматически создано {autoCategories.Count} событий.");
            }
        }
    }
}
