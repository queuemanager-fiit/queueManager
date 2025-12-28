using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Application.Interfaces;
using Domain.Entities;
using Microsoft.AspNetCore.Mvc;
using Moq;
using NUnit.Framework;
using WebApi.Controllers;

[TestFixture]
public class GroupControllerTests
{
    private Mock<IEventCategoryRepository> categoriesMock;
    private Mock<IGroupRepository> groupsMock;
    private Mock<IUnitOfWork> uowMock;
    private BotGroupController controller;

    [SetUp]
    public void SetUp()
    {
        categoriesMock = new();
        groupsMock = new();
        uowMock = new();
        controller = new BotGroupController(groupsMock.Object, categoriesMock.Object, uowMock.Object);
    }
    
    [Test]
    public async Task GetUsers_GroupNotFound_ReturnsNotFound_WithMessage()
    {
        var groupCode = "G1";
        var ct = CancellationToken.None;

        groupsMock
            .Setup(r => r.GetByCodeAsync(groupCode, ct))
            .ReturnsAsync((Group)null);

        var result = await controller.GetUsers(groupCode, ct);

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        var notFound = (NotFoundObjectResult)result.Result!;
        Assert.That(notFound.Value, Is.EqualTo($"Group with Group Code {groupCode} not found"));

        groupsMock.Verify(r => r.GetByCodeAsync(groupCode, ct), Times.Once);
    }

    [Test]
    public async Task GetUsers_GroupFound_ReturnsOk_WithTelegramIds()
    {
        var groupCode = "G1";
        var ct = CancellationToken.None;

        var group = new Group(groupCode);
        group.AddUser(1001);
        group.AddUser(2002);

        groupsMock
            .Setup(r => r.GetByCodeAsync(groupCode, ct))
            .ReturnsAsync(group);

        var result = await controller.GetUsers(groupCode, ct);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var ids = (List<long>)ok.Value!;

        CollectionAssert.AreEquivalent(new List<long> { 1001, 2002 }, ids);

        groupsMock.Verify(r => r.GetByCodeAsync(groupCode, ct), Times.Once);
    }
    
    [Test]
    public async Task DeleteCategory_Success_RemovesCategoryFromGroup_DeletesCategory_Saves_ReturnsNoContent()
    {
        var ct = CancellationToken.None;
        var dto = new BotGroupController.DeletionDto
        (
            GroupCode: "G1",
            CategoryName: "MatanchikDesk"
        );

        var group = new Group(dto.GroupCode);

        var category = new EventCategory(dto.CategoryName, false, dto.GroupCode);
        group.AddCategory(category.Id);

        groupsMock
            .Setup(r => r.GetByCodeAsync(dto.GroupCode, ct))
            .ReturnsAsync(group);

        categoriesMock
            .Setup(r => r.GetByGroupIdAndNameAsync(dto.GroupCode, dto.CategoryName, ct))
            .ReturnsAsync(category);

        groupsMock
            .Setup(r => r.UpdateAsync(group, ct))
            .Returns(Task.CompletedTask);

        categoriesMock
            .Setup(r => r.DeleteAsync(category, ct))
            .Returns(Task.CompletedTask);

        uowMock
            .Setup(r => r.SaveChangesAsync(ct))
            .ReturnsAsync(1);

        var result = await controller.DeleteCategory(dto, ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());

        Assert.That(group.CategoriesIds, Does.Not.Contain(category.Id));

        groupsMock.Verify(r => r.UpdateAsync(group, ct), Times.Once);
        categoriesMock.Verify(r => r.DeleteAsync(category, ct), Times.Once);
        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    [Test]
    public async Task DeleteCategory_GroupIsNull_ThrowsNullReference()
    {
        var ct = CancellationToken.None;
        var dto = new BotGroupController.DeletionDto("52", "CP");

        groupsMock
            .Setup(r => r.GetByCodeAsync(dto.GroupCode, ct))
            .ReturnsAsync((Group)null);

        var result = await controller.DeleteCategory(dto, ct);
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFound = (NotFoundObjectResult)result;
        Assert.That(notFound.Value, Is.EqualTo($"Group with Group Code {dto.GroupCode} not found"));
        
        groupsMock.Verify(r => r.UpdateAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Never);
        categoriesMock.Verify(r => r.DeleteAsync(It.IsAny<EventCategory>(), It.IsAny<CancellationToken>()), Times.Never);
        uowMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        groupsMock.VerifyAll();
        categoriesMock.VerifyNoOtherCalls();
        uowMock.VerifyNoOtherCalls();
    }

    [Test]
    public async Task DeleteCategory_CategoryIsNull_ThrowsNullReference()
    {
        var ct = CancellationToken.None;
        var dto = new BotGroupController.DeletionDto("52", "CP");

        groupsMock
            .Setup(r => r.GetByCodeAsync(dto.GroupCode, ct))
            .ReturnsAsync(new Group(dto.GroupCode));

        categoriesMock
            .Setup(r => r.GetByGroupIdAndNameAsync(dto.GroupCode, dto.CategoryName, ct))
            .ReturnsAsync((EventCategory)null);
        
        var result = await controller.DeleteCategory(dto, ct);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFound = (NotFoundObjectResult)result;
        Assert.That(notFound.Value, Is.EqualTo($"category with Category Name {dto.CategoryName} not found in group {dto.GroupCode}"));

        groupsMock.Verify(r => r.UpdateAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Never);
        categoriesMock.Verify(r => r.DeleteAsync(It.IsAny<EventCategory>(), It.IsAny<CancellationToken>()), Times.Never);
        uowMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        groupsMock.VerifyAll();
        categoriesMock.VerifyAll();
        uowMock.VerifyNoOtherCalls();
    }
    
    [Test]
    public async Task AddCategory_GroupNotFound_ReturnsNotFound_AndDoesNothingElse()
    {
        var ct = CancellationToken.None;
        var dto = new BotGroupController.CategoryDto
        (
            GroupCode: "G1",
            NewCategoryName: "CP",
            IsAutoCreate: true
        );

        groupsMock
            .Setup(r => r.GetByCodeAsync(dto.GroupCode, ct))
            .ReturnsAsync((Group)null);

        var result = await controller.AddCategory(dto, ct);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFound = (NotFoundObjectResult)result;
        Assert.That(notFound.Value, Is.EqualTo($"Group with Group Code {dto.GroupCode} not found"));

        groupsMock.Verify(r => r.UpdateAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Never);
        categoriesMock.Verify(r => r.AddAsync(It.IsAny<EventCategory>(), It.IsAny<CancellationToken>()), Times.Never);
        uowMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Test]
    public async Task AddCategory_Success_AddsCategoryToGroup_PersistsAndReturnsNoContent()
    {
        var ct = CancellationToken.None;
        var dto = new BotGroupController.CategoryDto
        (
            GroupCode: "G1",
            NewCategoryName: "CP",
            IsAutoCreate: true
        );

        var group = new Group(dto.GroupCode);

        groupsMock
            .Setup(r => r.GetByCodeAsync(dto.GroupCode, ct))
            .ReturnsAsync(group);

        groupsMock
            .Setup(r => r.UpdateAsync(group, ct))
            .Returns(Task.CompletedTask);

        categoriesMock
            .Setup(r => r.AddAsync(It.IsAny<EventCategory>(), ct))
            .Returns(Task.CompletedTask);

        uowMock
            .Setup(r => r.SaveChangesAsync(ct))
            .ReturnsAsync(1);

        var result = await controller.AddCategory(dto, ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());

        Assert.That(group.CategoriesIds.Count, Is.EqualTo(1));

        groupsMock.Verify(r => r.UpdateAsync(group, ct), Times.Once);
        categoriesMock.Verify(r => r.AddAsync(
                It.Is<EventCategory>(c =>
                    c.SubjectName == dto.NewCategoryName &&
                    c.IsAutoCreate == dto.IsAutoCreate &&
                    c.GroupCode == dto.GroupCode
                ),
                ct),
            Times.Once);

        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    [Test]
    public async Task GetCategories_GroupNotFound_ReturnsNotFound()
    {
        var ct = CancellationToken.None;
        var groupCode = "G1";

        groupsMock
            .Setup(r => r.GetByCodeAsync(groupCode, ct))
            .ReturnsAsync((Group)null);

        var result = await controller.GetCategories(groupCode, ct);

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        var notFound = (NotFoundObjectResult)result.Result!;
        Assert.That(notFound.Value, Is.EqualTo($"Group with Group Code {groupCode} not found"));

        groupsMock.Verify(r => r.GetByCodeAsync(groupCode, ct), Times.Once);
        categoriesMock.Verify(
            r => r.GetByIdAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Test]
    public async Task GetCategories_GroupFound_ReturnsOnlyExistingCategoryNames()
    {
        var ct = CancellationToken.None;
        var groupCode = "G1";
        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();
        var id3 = Guid.NewGuid();

        var group = new Group(groupCode);
        group.AddCategory(id1);
        group.AddCategory(id2);
        group.AddCategory(id3);

        groupsMock
            .Setup(r => r.GetByCodeAsync(groupCode, ct))
            .ReturnsAsync(group);

        categoriesMock
            .Setup(r => r.GetByIdAsync(id1, ct))
            .ReturnsAsync(new EventCategory("CP", false, groupCode));

        categoriesMock
            .Setup(r => r.GetByIdAsync(id2, ct))
            .ReturnsAsync((EventCategory)null); // категория не найдена

        categoriesMock
            .Setup(r => r.GetByIdAsync(id3, ct))
            .ReturnsAsync(new EventCategory("task", false, groupCode));

        var result = await controller.GetCategories(groupCode, ct);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var names = (List<string>)ok.Value!;

        CollectionAssert.AreEquivalent(
            new[] { "CP", "task" },
            names);

        groupsMock.Verify(r => r.GetByCodeAsync(groupCode, ct), Times.Once);
        categoriesMock.Verify(r => r.GetByIdAsync(It.IsAny<Guid>(), ct), Times.Exactly(3));
    }
}