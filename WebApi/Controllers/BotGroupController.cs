using Domain.Entities;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;


[ApiController]
[Route("api/groups")]
public class BotGroupController : ControllerBase
{
    private readonly IGroupRepository groups;
    private readonly IEventCategoryRepository eventCategories;
    private readonly IUnitOfWork uow;
    
    public BotGroupController(IGroupRepository groups , IEventCategoryRepository eventCategories, IUnitOfWork uow)
    {
        this.groups = groups;
        this.eventCategories = eventCategories;
        this.uow = uow;
    }
    
    public sealed record CategoryDto(
        string GroupCode,
        bool IsAutoCreate,
        string NewCategoryName);
    
    public sealed record DeletionDto(
        string GroupCode,
        string CategoryName);
    
    //вовзращает список категорий для указанной группы/подгруппы
    [HttpGet("category-list")]
    public async Task<ActionResult<List<string>>> GetCategories(
        [FromQuery] string groupCode,
        CancellationToken ct)
    {
        return Ok((await groups.GetByCodeAsync(groupCode, ct)).CategoriesIds.Select(categ => (await eventCategories.)).ToList());
    }
    
    //добавляет категорию для указанной группы/подгруппы
    [HttpPost("add-category")]
    public async Task<IActionResult> AddCategory(
        [FromBody] CategoryDto dto,
        CancellationToken ct)
    {
        var group = await groups.GetByCodeAsync(dto.GroupCode, ct);
        var newCategory = new EventCategory(dto.NewCategoryName, dto.IsAutoCreate, dto.GroupCode);
        group.AddCategory(newCategory);
        await groups.UpdateAsync(group, ct);
        await eventCategories.AddAsync(newCategory, ct);
        
        await uow.SaveChangesAsync(ct);
        return NoContent();
    }
    
    //удаляет категорию для указанной группы/подгруппы
    [HttpPost("delete-category")]
    public async Task<IActionResult> DeleteCategory(
        [FromBody] DeletionDto dto,
        CancellationToken ct)
    {
        var group = await groups.GetByCodeAsync(dto.GroupCode, ct);
        var category = await eventCategories.GetByGroupIdAndNameAsync(dto.GroupCode, dto.CategoryName, ct);
        group.RemoveCategory(category);
        await groups.UpdateAsync(group, ct);
        await eventCategories.DeleteAsync(category, ct);
        
        await uow.SaveChangesAsync(ct);
        return NoContent();
    }

    //возвращает тг id всех студентов в группе
    [HttpGet("users-for-group")]
    public async Task<ActionResult<List<long>>> GetUsers([FromQuery] string groupCode, CancellationToken ct)
    {
        var group = await groups.GetByCodeAsync(groupCode, ct);
        return Ok(group.UsersTelegramIds.Select(user => user.TelegramId).ToList());
    }
}
