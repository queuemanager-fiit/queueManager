public class QueueService : IQueueService
{
    private const int FormationOffsetHours = 24;
    private const int DeletionOffsetHours = 24;
    
    private readonly IEventRepository _eventRepository;
    private readonly IEventCategoryRepository _categoryRepository;
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _uow;

    public QueueService(
        IEventRepository eventRepository,
        IEventCategoryRepository categoryRepository,
        IUserRepository userRepository,
        IUnitOfWork uow)
    {
        _eventRepository = eventRepository;
        _categoryRepository = categoryRepository;
        _userRepository = userRepository;
        _uow = uow;
    }

    public async Task CreateAutomaticQueuesAsync(List<Group> groups, CancellationToken ct)
    {
        var categories = await _categoryRepository.GetAutoCreateCategoriesAsync(ct);
        
        foreach (var group in groups)
        {
            foreach (var lesson in group.Lessons)
            {
                // Фильтруем категории только для текущей группы
                var category = categories
                    .FirstOrDefault(c => 
                        c.SubjectName == lesson.Name && 
                        c.GroupId == group.Id);
                
                if (category == null) continue;
                
                var formationTime = lesson.DateTime.AddHours(-FormationOffsetHours);
                var deletionTime = lesson.DateTime.AddHours(DeletionOffsetHours);
                
                var queue = new Event(
                    category,
                    lesson.DateTime,
                    formationTime,
                    deletionTime,
                    group.Id);
                
                await _eventRepository.AddAsync(queue, ct);
            }
        }
        
        await _uow.SaveChangesAsync(ct);
    }

    public async Task FormQueueAsync(Guid queueId, CancellationToken ct)
    {
        var queue = await _eventRepository.GetByIdAsync(queueId, ct);
        if (queue == null) return;
        
        // Сортировка пользователей по предпочтениям
        var sortedUsers = SortUsers(queue.Users);
        
        
        queue.State = QueueState.Formed;
        await _eventRepository.UpdateAsync(queue, ct);
    }

    private List<User> SortUsers(List<User> users)
    {
        return users
            .OrderBy(u => u.Preference == UserPreference.Start)
            .ThenBy(u => u.Preference == UserPreference.NoPreference)
            .ThenBy(u => u.Preference == UserPreference.End)
            .ToList();
    }

    public async Task AddUserToQueueAsync(Guid queueId, long telegramId, UserPreference preference, CancellationToken ct)
    {
        var queue = await _eventRepository.GetByIdAsync(queueId, ct);
        var user = await _userRepository.GetByTelegramIdAsync(telegramId, ct);
        
        if (queue == null || user == null) return;
        
        // Проверка принадлежности к группе
        if (user.GroupId != queue.GroupId)
            throw new Exception("Пользователь не принадлежит к группе очереди");

        if (!queue.Users.Any(u => u.TelegramId == telegramId))
        {
            user.SetPreference(preference);
            queue.Users.Add(user);
            
            if (queue.State == QueueState.Formed)
            {
                await FormQueueAsync(queueId, ct);
            }
            
            await _eventRepository.UpdateAsync(queue, ct);
        }
    }
}