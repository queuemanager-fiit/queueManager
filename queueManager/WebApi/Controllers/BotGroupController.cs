using Domain.Entities;
using Application.Interfaces;
using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;


[ApiController]
[Route("api/groups")]
public class BotGroupController : ControllerBase
{
    private readonly IGroupRepository groups;
    private readonly IUnitOfWork uow;
    
    public BotGroupController(IGroupRepository groups , IUnitOfWork uow)
    {
        this.groups = groups;
        this.uow = uow;
    }
    
    public sealed record CategoryDto(
        string GroupCode,
        bool IsAutoCreate,
        string NewCategoryName);
    
    //вовзращает список категорий для указанной группы/подгруппы
    [HttpGet("category-list")]
    public async Task<ActionResult<List<string>>> GetCategories(
        [FromQuery] string groupCode,
        CancellationToken ct) =>
        Ok((await groups.GetByCodeAsync(groupCode, ct)).GetCategories().Select(categ => categ.SubjectName).ToList());
    
    //добавляет категорию для указанной группы/подгруппы
    [HttpPost("add-category")]
    public async Task<IActionResult> AddCategory(
        [FromBody] CategoryDto dto,
        CancellationToken ct)
    {
        var groupBase = await groups.GetByCodeAsync(dto.GroupCode, ct);
        groupBase.AddCategory(new EventCategory(dto.NewCategoryName, dto.IsAutoCreate, dto.GroupCode));
        groups.UpdateAsync(groupBase, ct);
        
        await uow.SaveChangesAsync(ct);
        return NoContent();
    }
}