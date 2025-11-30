namespace QueueManagerBot
{
    public enum UserState
    {
        None,
        WaitingForStudentData,
        WaitingForQueueName,
        WaitingForQueueCategory,
        WaitingForQueueNameToDelete,
        WaitingForNewCategoryName
    }
}