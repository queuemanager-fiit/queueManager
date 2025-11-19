namespace Domain.Entities;

public class Group
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Code { get; private set; }
    
    public Group(string code) =>
        Code = code;
}