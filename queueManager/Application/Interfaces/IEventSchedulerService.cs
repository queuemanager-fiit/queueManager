using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    public interface IEventSchedulerService
    {
        Task CreateEventsFromScheduleAsync(IEnumerable<ScheduleEntry> scheduleEntries, CancellationToken ct);
        Task FormQueuesForDueEventsAsync(DateTimeOffset now, CancellationToken ct);
    }

    public class ScheduleEntry
    {
        public string SubjectName { get; set; }
        public string GroupCode { get; set; }
        public int? SubgroupNumber { get; set; } // null = вся группа
        public DateTimeOffset OccurredOn { get; set; }
    }
}
