using System.ComponentModel.DataAnnotations;
using System.Collections.Generic;

namespace Domain.Entities;

public class EventCategory
{
    [Key]
    public Guid Id { get; private set; } = Guid.NewGuid();
    public string SubjectName { get; private set;}
    public bool IsAutoCreate { get; private set;}
    public string GroupCode { get; private set;}
    public List<long> UnfinishedUsersTelegramIds { get; private set; }= new();

    public EventCategory(
        string subjectName,
        bool isAutoCreate,
        string groupCode)
    {
        SubjectName = subjectName ?? throw new ArgumentNullException(nameof(subjectName));
        IsAutoCreate = isAutoCreate;
        GroupCode = groupCode ?? throw new ArgumentNullException(nameof(groupCode));
    }

    public void UpdateUnfinishedUsers(IReadOnlyList<long> queue, int position)
    {
        if (queue == null)
            throw new ArgumentNullException(nameof(queue));

        int cutoffPosition = position - 1;

        if (cutoffPosition < 0)
            throw new ArgumentOutOfRangeException(
                nameof(position),
                "Номер позиции должен быть положительным целым числом."
            );

        UnfinishedUsersTelegramIds.Clear();


        for (int i = cutoffPosition; i < queue.Count; i++)
        {
            var user = queue[i];
            UnfinishedUsersTelegramIds.Add(user);
        }
    }
    
    public override bool Equals(object? obj)
        => obj is EventCategory e && e.Id == Id;

    public override int GetHashCode()
        => Id.GetHashCode();
}