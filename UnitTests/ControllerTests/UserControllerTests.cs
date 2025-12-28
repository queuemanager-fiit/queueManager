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
public class UserControllerTests
{
    private Mock<IUserRepository> usersMock;
    private Mock<IGroupRepository> groupsMock;
    private Mock<IUnitOfWork> uowMock;
    private BotUserController controller;

    [SetUp]
    public void SetUp()
    {
        usersMock = new();
        groupsMock = new();
        uowMock = new();
        controller = new BotUserController(usersMock.Object, groupsMock.Object, uowMock.Object);
    }

    [Test]
    public async Task GetUserInfo_UserNotFound_ReturnsNotFound()
    {
        var telegramId = 88005553535;

        usersMock
            .Setup(u => u.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User)null);

        var result = await controller.GetUserInfo(telegramId, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());

        var notFound = (NotFoundObjectResult)result.Result!;
        Assert.That(notFound.Value, Is.EqualTo($"User with TelegramId {telegramId} not found"));
    }

    [Test]
    public async Task GetUserInfo_UserFound_ReturnsOkWithDto()
    {
        var telegramId = 88005553535;

        var user = new User(
            telegramId,
            fullName: "Ivan Ivanov",
            username: "vanya52",
            groupCodes: new List<string> { "G1", "G2" }
        );

        usersMock
            .Setup(u => u.GetByTelegramIdAsync(telegramId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await controller.GetUserInfo(telegramId, CancellationToken.None);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());

        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.InstanceOf<BotUserController.InfoUserDto>());

        var dto = (BotUserController.InfoUserDto)ok.Value!;
        Assert.That(dto.FullName, Is.EqualTo("Ivan Ivanov"));
        Assert.That(dto.Username, Is.EqualTo("vanya52"));
        Assert.That(dto.GroupCode, Is.EqualTo("G1"));
        Assert.That(dto.SubGroupCode, Is.EqualTo("G2"));
        Assert.That(dto.IsAdmin, Is.False);
        Assert.That(dto.AveragePosition, Is.EqualTo(0));
        Assert.That(dto.ParticipationCount, Is.EqualTo(0));
    }

    [Test]
    public async Task DeleteUserInfo_UserNotFound_ReturnsNotFound_AndDoesNotUpdateAnything()
    {
        var dto = new BotUserController.DeletionUserDto(TelegramId: 1001, GroupCode: "G1");
        var ct = CancellationToken.None;

        usersMock
            .Setup(r => r.GetByTelegramIdAsync(dto.TelegramId, ct))
            .ReturnsAsync((User)null);
        
        var result = await controller.DeleteUserInfo(dto, ct);
        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFound = (NotFoundObjectResult)result;
        Assert.That(notFound.Value, Is.EqualTo($"User with TelegramId {dto.TelegramId} not found"));
        
        usersMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        groupsMock.Verify(r => r.UpdateAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Never);
        uowMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        usersMock.VerifyAll();
        groupsMock.VerifyNoOtherCalls();
        uowMock.VerifyNoOtherCalls();
    }
    
    [Test]
    public async Task DeleteUserInfo_GroupNotFound_ReturnsNotFound_AndDoesNotUpdateAnything()
    {
        var dto = new BotUserController.DeletionUserDto(TelegramId: 1001, GroupCode: "G1");
        var ct = CancellationToken.None;

        var user = new User(dto.TelegramId, fullName: "Test", username: "test",
            groupCodes: new List<string> { "G1", "G2" });

        usersMock
            .Setup(r => r.GetByTelegramIdAsync(dto.TelegramId, ct))
            .ReturnsAsync(user);

        groupsMock
            .Setup(r => r.GetByCodeAsync(dto.GroupCode, ct))
            .ReturnsAsync((Group)null);

        var result = await controller.DeleteUserInfo(dto, ct);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var notFound = (NotFoundObjectResult)result;
        Assert.That(notFound.Value, Is.EqualTo($"Group with Group Code {dto.GroupCode} not found"));

        usersMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        groupsMock.Verify(r => r.UpdateAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Never);
        uowMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);

        usersMock.VerifyAll();
        groupsMock.VerifyAll();
        uowMock.VerifyNoOtherCalls();
    }
    
    [Test]
    public async Task DeleteUserInfo_Success_RemovesFromGroup_UpdatesBoth_Saves_AndReturnsNoContent()
    {
        var dto = new BotUserController.DeletionUserDto(TelegramId: 1001, GroupCode: "G1");
        var ct = CancellationToken.None;

        var user = new User(dto.TelegramId, fullName: "Test", username: "test",
            groupCodes: new List<string> { "G1", "G2" });

        var group = new Group(dto.GroupCode);
        group.AddUser(dto.TelegramId);
        group.AddUser(2333);

        usersMock
            .Setup(r => r.GetByTelegramIdAsync(dto.TelegramId, ct))
            .ReturnsAsync(user);

        groupsMock
            .Setup(r => r.GetByCodeAsync(dto.GroupCode, ct))
            .ReturnsAsync(group);

        groupsMock
            .Setup(r => r.UpdateAsync(group, ct))
            .Returns(Task.CompletedTask);

        usersMock
            .Setup(r => r.UpdateAsync(user, ct))
            .Returns(Task.CompletedTask);

        uowMock
            .Setup(r => r.SaveChangesAsync(ct))
            .ReturnsAsync(1);

        var result = await controller.DeleteUserInfo(dto, ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());

        Assert.That(user.GroupCodes, Does.Not.Contain(dto.GroupCode));
        Assert.That(group.UsersTelegramIds, Does.Not.Contain(dto.TelegramId));

        groupsMock.Verify(r => r.UpdateAsync(group, ct), Times.Once);
        usersMock.Verify(r => r.UpdateAsync(user, ct), Times.Once);
        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);

        usersMock.VerifyAll();
        groupsMock.VerifyAll();
        uowMock.VerifyAll();
    }
    
    [Test]
    public async Task UpdateUserInfo_GroupAndSubGroupDontExist_CreatesBoth_CreatesUser_AddsUserToBoth_ReturnsNoContent()
    {
        var ct = CancellationToken.None;
        var dto = new BotUserController.BotUserDto
        (
            TelegramId: 1001,
            FullName: "Test User",
            Username: "test",
            GroupCode: "G1",
            SubGroupCode: "G2"
        );

        groupsMock.Setup(r => r.GetByCodeAsync(dto.GroupCode, ct)).ReturnsAsync((Group)null);
        groupsMock.Setup(r => r.GetByCodeAsync(dto.SubGroupCode, ct)).ReturnsAsync((Group)null);

        groupsMock.Setup(r => r.AddAsync(It.IsAny<Group>(), ct)).Returns(Task.CompletedTask);
        usersMock.Setup(r => r.GetByTelegramIdAsync(dto.TelegramId, ct)).ReturnsAsync((User)null);
        usersMock.Setup(r => r.AddAsync(It.IsAny<User>(), ct)).Returns(Task.CompletedTask);

        groupsMock.Setup(r => r.UpdateAsync(It.IsAny<Group>(), ct)).Returns(Task.CompletedTask);
        uowMock.Setup(r => r.SaveChangesAsync(ct)).ReturnsAsync(1);

        var result = await controller.UpdateUserInfo(dto, ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());

        groupsMock.Verify(r => r.AddAsync(It.Is<Group>(g => g.Code == dto.GroupCode), ct), Times.Once);
        groupsMock.Verify(r => r.AddAsync(It.Is<Group>(g => g.Code == dto.SubGroupCode), ct), Times.Once);

        usersMock.Verify(r => r.AddAsync(It.Is<User>(u =>
            u.TelegramId == dto.TelegramId &&
            u.FullName == dto.FullName &&
            u.Username == dto.Username &&
            u.GroupCodes.Count == 2 &&
            u.GroupCodes[0] == dto.GroupCode &&
            u.GroupCodes[1] == dto.SubGroupCode
        ), ct), Times.Once);

        groupsMock.Verify(r => r.UpdateAsync(It.Is<Group>(g => g.Code == dto.GroupCode), ct), Times.Once);
        groupsMock.Verify(r => r.UpdateAsync(It.Is<Group>(g => g.Code == dto.SubGroupCode), ct), Times.Once);

        usersMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);

        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    [Test]
    public async Task UpdateUserInfo_GroupExists_SubGroupMissing_CreatesSubGroup_CreatesUser_ReturnsNoContent()
    {
        var ct = CancellationToken.None;
        var dto = new BotUserController.BotUserDto
        (
            TelegramId: 1001,
            FullName: "Test User",
            Username: "test",
            GroupCode: "G1",
            SubGroupCode: "G2"
        );

        var existingGroup = new Group(dto.GroupCode);
        existingGroup.AddUser(777);

        groupsMock.Setup(r => r.GetByCodeAsync(dto.GroupCode, ct)).ReturnsAsync(existingGroup);
        groupsMock.Setup(r => r.GetByCodeAsync(dto.SubGroupCode, ct)).ReturnsAsync((Group)null);

        groupsMock.Setup(r => r.AddAsync(It.IsAny<Group>(), ct)).Returns(Task.CompletedTask);
        usersMock.Setup(r => r.GetByTelegramIdAsync(dto.TelegramId, ct)).ReturnsAsync((User)null);
        usersMock.Setup(r => r.AddAsync(It.IsAny<User>(), ct)).Returns(Task.CompletedTask);

        groupsMock.Setup(r => r.UpdateAsync(It.IsAny<Group>(), ct)).Returns(Task.CompletedTask);
        uowMock.Setup(r => r.SaveChangesAsync(ct)).ReturnsAsync(1);

        var result = await controller.UpdateUserInfo(dto, ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());

        groupsMock.Verify(r => r.AddAsync(It.Is<Group>(g => g.Code == dto.SubGroupCode), ct), Times.Once);
        groupsMock.Verify(r => r.AddAsync(It.Is<Group>(g => g.Code == dto.GroupCode), ct), Times.Never);

        usersMock.Verify(r => r.AddAsync(It.IsAny<User>(), ct), Times.Once);
        usersMock.Verify(r => r.UpdateAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);

        groupsMock.Verify(r => r.UpdateAsync(It.Is<Group>(g => g.Code == dto.GroupCode), ct), Times.Once);
        groupsMock.Verify(r => r.UpdateAsync(It.Is<Group>(g => g.Code == dto.SubGroupCode), ct), Times.Once);

        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    [Test]
    public async Task UpdateUserInfo_UserExists_ChangesBothGroups_RemovesFromOld_AddsToNew_UpdatesUser_AndUpdatesGroups_ReturnsNoContent()
    {
        var ct = CancellationToken.None;
        var dto = new BotUserController.BotUserDto
        (
            TelegramId: 1001,
            FullName: "Test User",
            Username: "test",
            GroupCode: "G1",
            SubGroupCode: "G2"
        );

        var newGroup = new Group(dto.GroupCode);
        var newSubGroup = new Group(dto.SubGroupCode);

        var oldGroup = new Group("G_OLD");
        oldGroup.AddUser(dto.TelegramId);
        oldGroup.AddUser(2222);

        var oldSubGroup = new Group("S_OLD");
        oldSubGroup.AddUser(dto.TelegramId);
        oldSubGroup.AddUser(3333);

        var user = new User(dto.TelegramId, "Old Name", "olduser",
            new List<string> { "G_OLD", "S_OLD" });

        groupsMock.Setup(r => r.GetByCodeAsync(dto.GroupCode, ct)).ReturnsAsync(newGroup);
        groupsMock.Setup(r => r.GetByCodeAsync(dto.SubGroupCode, ct)).ReturnsAsync(newSubGroup);

        usersMock.Setup(r => r.GetByTelegramIdAsync(dto.TelegramId, ct)).ReturnsAsync(user);

        groupsMock.Setup(r => r.GetByCodeAsync("G_OLD", ct)).ReturnsAsync(oldGroup);
        groupsMock.Setup(r => r.GetByCodeAsync("S_OLD", ct)).ReturnsAsync(oldSubGroup);

        usersMock.Setup(r => r.UpdateAsync(user, ct)).Returns(Task.CompletedTask);

        groupsMock.Setup(r => r.UpdateAsync(It.IsAny<Group>(), ct)).Returns(Task.CompletedTask);
        uowMock.Setup(r => r.SaveChangesAsync(ct)).ReturnsAsync(1);

        var result = await controller.UpdateUserInfo(dto, ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());

        Assert.That(oldGroup.UsersTelegramIds, Does.Not.Contain(dto.TelegramId));
        Assert.That(oldSubGroup.UsersTelegramIds, Does.Not.Contain(dto.TelegramId));
        Assert.That(newGroup.UsersTelegramIds, Does.Contain(dto.TelegramId));
        Assert.That(newSubGroup.UsersTelegramIds, Does.Contain(dto.TelegramId));

        Assert.That(user.FullName, Is.EqualTo(dto.FullName));
        Assert.That(user.Username, Is.EqualTo(dto.Username));
        Assert.That(user.GroupCodes[0], Is.EqualTo(dto.GroupCode));
        Assert.That(user.GroupCodes[1], Is.EqualTo(dto.SubGroupCode));

        usersMock.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
        usersMock.Verify(r => r.UpdateAsync(user, ct), Times.Once);

        groupsMock.Verify(r => r.AddAsync(It.IsAny<Group>(), It.IsAny<CancellationToken>()), Times.Never);
        groupsMock.Verify(r => r.UpdateAsync(oldGroup, ct), Times.Once);
        groupsMock.Verify(r => r.UpdateAsync(oldSubGroup, ct), Times.Once);
        groupsMock.Verify(r => r.UpdateAsync(newGroup, ct), Times.Once);
        groupsMock.Verify(r => r.UpdateAsync(newSubGroup, ct), Times.Once);

        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
}