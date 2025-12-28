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
public class EventControllerTests
{
    private Mock<IEventCategoryRepository> categoriesMock;
    private Mock<IGroupRepository> groupsMock;
    private Mock<IUserRepository> usersMock;
    private Mock<IEventRepository> eventsMock;
    private Mock<IUnitOfWork> uowMock;
    private BotEventController controller;

    [SetUp]
    public void SetUp()
    {
        categoriesMock = new();
        groupsMock = new();
        usersMock  = new();
        eventsMock = new();
        uowMock = new();
        controller = new BotEventController(usersMock.Object, eventsMock.Object, categoriesMock.Object, groupsMock.Object, uowMock.Object);
    }
    
    [Test]
    public async Task GetDueNotification_ReturnsOk_WithDtos()
    {
        var ct = CancellationToken.None;

        var category = new EventCategory(subjectName: "CP", isAutoCreate: false, groupCode: "G1");
        
        var ev = new Event(category: category.Id, occurredOn: DateTimeOffset.UtcNow.AddHours(1), groupCode: "G1");
        ev.ParticipantsTelegramIds.AddRange(new[] { 1L, 2L });

        eventsMock
            .Setup(r => r.GetDueNotificationAsync(It.IsAny<DateTimeOffset>(), ct))
            .ReturnsAsync(new List<Event> { ev });

        categoriesMock
            .Setup(r => r.GetByIdAsync(ev.CategoryId, ct))
            .ReturnsAsync(category);

        var result = await controller.GetDueNotification(ct);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var dtos = (List<BotEventController.BotEventDto>)ok.Value!;

        Assert.That(dtos.Count, Is.EqualTo(1));
        Assert.That(dtos[0].EventId, Is.EqualTo(ev.Id));
        Assert.That(dtos[0].GroupCode, Is.EqualTo("G1"));

        eventsMock.Verify(r => r.GetDueNotificationAsync(It.IsAny<DateTimeOffset>(), ct), Times.Once);
    }
    
    [Test]
    public async Task GetDueFormation_SavesChanges_AndReturnsOk()
    {
        var ct = CancellationToken.None;
        var id  = Guid.NewGuid();

        var ev = new Event(category: id, occurredOn: DateTimeOffset.UtcNow.AddMinutes(10), groupCode: "G1");
        ev.IsFormed = true;

        eventsMock
            .Setup(r => r.GetDueFormationAsync(It.IsAny<DateTimeOffset>(), ct))
            .ReturnsAsync(new List<Event> { ev });

        categoriesMock
            .Setup(r => r.GetByIdAsync(ev.CategoryId, ct))
            .ReturnsAsync(new EventCategory("CP", false, "G1"));

        uowMock.Setup(r => r.SaveChangesAsync(ct)).ReturnsAsync(1);

        var result = await controller.GetDueFormation(ct);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    [Test]
    public async Task MarkNotified_EmptyIds_ReturnsBadRequest()
    {
        var ct = CancellationToken.None;

        var result = await controller.MarkNotified(new BotEventController.MarkNotifiedEvents(new List<Guid>()), ct);

        Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        var bad = (BadRequestObjectResult)result;
        Assert.That(bad.Value, Is.EqualTo("No ids provided"));

        uowMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
        eventsMock.Verify(r => r.UpdateAsync(It.IsAny<Event>(), It.IsAny<CancellationToken>()), Times.Never);
    }
    
    [Test]
    public async Task MarkNotified_WithIds_UpdatesEach_AndSavesOnce_ReturnsNoContent()
    {
        var ct = CancellationToken.None;

        var id1 = Guid.NewGuid();
        var id2 = Guid.NewGuid();

        var ev1 = new Event(id1, DateTimeOffset.UtcNow, "G1");
        var ev2 = new Event(id2, DateTimeOffset.UtcNow, "G1");

        eventsMock.Setup(r => r.GetByIdAsync(ev1.Id, ct)).ReturnsAsync(ev1);
        eventsMock.Setup(r => r.GetByIdAsync(ev2.Id, ct)).ReturnsAsync(ev2);
        eventsMock.Setup(r => r.UpdateAsync(It.IsAny<Event>(), ct)).Returns(Task.CompletedTask);
        uowMock.Setup(r => r.SaveChangesAsync(ct)).ReturnsAsync(1);

        var result = await controller.MarkNotified(new BotEventController.MarkNotifiedEvents(new List<Guid> { ev1.Id, ev2.Id }), ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());

        eventsMock.Verify(r => r.UpdateAsync(ev1, ct), Times.Once);
        eventsMock.Verify(r => r.UpdateAsync(ev2, ct), Times.Once);
        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    [Test]
    public async Task Confirm_AddsParticipant_UpdatesEvent_Saves_ReturnsNoContent()
    {
        var ct = CancellationToken.None;

        var user = new User(telegramId: 1001, fullName: "A", username: "a", groupCodes: new List<string> { "G1", "SG1" });
        var ev = new Event(Guid.NewGuid(), DateTimeOffset.UtcNow, "G1");

        usersMock.Setup(r => r.GetByTelegramIdAsync(1001, ct)).ReturnsAsync(user);
        eventsMock.Setup(r => r.GetByIdAsync(ev.Id, ct)).ReturnsAsync(ev);
        eventsMock.Setup(r => r.UpdateAsync(ev, ct)).Returns(Task.CompletedTask);
        uowMock.Setup(r => r.SaveChangesAsync(ct)).ReturnsAsync(1);

        var dto = new BotEventController.ParticipationDto(1001, ev.Id, UserPreference.Start);

        var result = await controller.Confirm(dto, ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        eventsMock.Verify(r => r.UpdateAsync(ev, ct), Times.Once);
        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    [Test]
    public async Task QuitQueue_RemovesParticipant_UpdatesEvent_Saves_ReturnsNoContent()
    {
        var ct = CancellationToken.None;

        var user = new User(telegramId: 1001, fullName: "A", username: "a", groupCodes: new List<string> { "G1", "SG1" });
        var ev = new Event(Guid.NewGuid(), DateTimeOffset.UtcNow, "G1");

        usersMock.Setup(r => r.GetByTelegramIdAsync(1001, ct)).ReturnsAsync(user);
        eventsMock.Setup(r => r.GetByIdAsync(ev.Id, ct)).ReturnsAsync(ev);
        eventsMock.Setup(r => r.UpdateAsync(ev, ct)).Returns(Task.CompletedTask);
        uowMock.Setup(r => r.SaveChangesAsync(ct)).ReturnsAsync(1);

        var dto = new BotEventController.CancellationDto(1001, ev.Id);

        var result = await controller.QuitQueue(dto, ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        eventsMock.Verify(r => r.UpdateAsync(ev, ct), Times.Once);
        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    [Test]
    public async Task CreateQueue_AddsEvent_UpdatesGroup_Saves_ReturnsOkWithEventId()
    {
        var ct = CancellationToken.None;

        var group = new Group("G1");
        var category = new EventCategory("CP", false, "G1");

        groupsMock.Setup(r => r.GetByCodeAsync("G1", ct)).ReturnsAsync(group);
        categoriesMock.Setup(r => r.GetByGroupIdAndNameAsync("G1", "CP", ct)).ReturnsAsync(category);

        Event created = null!;
        eventsMock.Setup(r => r.AddAsync(It.IsAny<Event>(), ct))
            .Callback<Event, CancellationToken>((e, _) => created = e)
            .Returns(Task.CompletedTask);

        groupsMock.Setup(r => r.UpdateAsync(group, ct)).Returns(Task.CompletedTask);
        uowMock.Setup(r => r.SaveChangesAsync(ct)).ReturnsAsync(1);

        var dto = new BotEventController.CreationDto("G1", "CP", DateTimeOffset.UtcNow);

        var result = await controller.CreateQueue(dto, ct);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        Assert.That(ok.Value, Is.InstanceOf<Guid>());

        var returnedId = (Guid)ok.Value!;
        Assert.That(created, Is.Not.Null);
        Assert.That(returnedId, Is.EqualTo(created.Id));

        eventsMock.Verify(r => r.AddAsync(It.IsAny<Event>(), ct), Times.Once);
        groupsMock.Verify(r => r.UpdateAsync(group, ct), Times.Once);
        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    [Test]
    public async Task DeleteQueue_RemovesFromGroup_DeletesEvent_Saves_ReturnsNoContent()
    {
        var ct = CancellationToken.None;

        var group = new Group("G1");

        var ev = new Event(Guid.NewGuid(), DateTimeOffset.UtcNow, "G1");
        group.AddEvent(ev.Id);

        groupsMock.Setup(r => r.GetByCodeAsync("G1", ct)).ReturnsAsync(group);
        eventsMock.Setup(r => r.GetByIdAsync(ev.Id, ct)).ReturnsAsync(ev);

        groupsMock.Setup(r => r.UpdateAsync(group, ct)).Returns(Task.CompletedTask);
        eventsMock.Setup(r => r.DeleteAsync(ev, ct)).Returns(Task.CompletedTask);
        uowMock.Setup(r => r.SaveChangesAsync(ct)).ReturnsAsync(1);

        var dto = new BotEventController.DeletionDto("G1", ev.Id);

        var result = await controller.DeleteQueue(dto, ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());

        groupsMock.Verify(r => r.UpdateAsync(group, ct), Times.Once);
        eventsMock.Verify(r => r.DeleteAsync(ev, ct), Times.Once);
        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    [Test]
    public async Task GetForGroup_GroupNotFound_ReturnsNotFound()
    {
        var ct = CancellationToken.None;
        groupsMock.Setup(r => r.GetByCodeAsync("G1", ct)).ReturnsAsync((Group)null);

        var result = await controller.GetForGroup("G1", ct);

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        var nf = (NotFoundObjectResult)result.Result!;
        Assert.That(nf.Value, Is.EqualTo("Group with Group Code G1 not found"));
    }

    [Test]
    public async Task GetForGroup_GroupFound_ReturnsOkWithDtos()
    {
        var ct = CancellationToken.None;

        var group = new Group("G1");

        var ev1 = new Event(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-1), "G1");
        var ev2 = new Event(Guid.NewGuid(), DateTimeOffset.UtcNow.AddHours(-2), "G1");
        group.AddEvent(ev1.Id);
        group.AddEvent(ev2.Id);

        groupsMock.Setup(r => r.GetByCodeAsync("G1", ct)).ReturnsAsync(group);
        eventsMock.Setup(r => r.GetByIdAsync(ev1.Id, ct)).ReturnsAsync(ev1);
        eventsMock.Setup(r => r.GetByIdAsync(ev2.Id, ct)).ReturnsAsync(ev2);

        categoriesMock.Setup(r => r.GetByIdAsync(ev1.CategoryId, ct)).ReturnsAsync(new EventCategory("Sport", false, "G1"));
        categoriesMock.Setup(r => r.GetByIdAsync(ev2.CategoryId, ct)).ReturnsAsync(new EventCategory("Sport", false, "G1"));

        var result = await controller.GetForGroup("G1", ct);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var dtos = (List<BotEventController.BotEventDto>)ok.Value!;
        Assert.That(dtos.Count, Is.EqualTo(2));
    }
    
    [Test]
    public async Task MarkUnfinished_CategoryNotFound_ReturnsNotFound()
    {
        var ct = CancellationToken.None;

        var ev = new Event(Guid.NewGuid(), DateTimeOffset.UtcNow, "G1");

        eventsMock.Setup(r => r.GetByIdAsync(ev.Id, ct)).ReturnsAsync(ev);
        categoriesMock.Setup(r => r.GetByIdAsync(ev.CategoryId, ct)).ReturnsAsync((EventCategory)null);

        var result = await controller.MarkUnfinished(new BotEventController.MarkUnfinishedUsers(ev.Id, 2), ct);

        Assert.That(result, Is.InstanceOf<NotFoundObjectResult>());
        var nf = (NotFoundObjectResult)result;
        Assert.That(nf.Value, Is.EqualTo($"Category with Id {ev.CategoryId} not found"));

        categoriesMock.Verify(r => r.UpdateAsync(It.IsAny<EventCategory>(), It.IsAny<CancellationToken>()), Times.Never);
        uowMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task MarkUnfinished_Success_UpdatesCategory_Saves_ReturnsNoContent()
    {
        var ct = CancellationToken.None;

        var ev = new Event(Guid.NewGuid(), DateTimeOffset.UtcNow, "G1");
        ev.ParticipantsTelegramIds.AddRange(new[] { 1L, 2L, 3L });

        var category = new EventCategory("Sport", false, "G1");

        eventsMock.Setup(r => r.GetByIdAsync(ev.Id, ct)).ReturnsAsync(ev);
        categoriesMock.Setup(r => r.GetByIdAsync(ev.CategoryId, ct)).ReturnsAsync(category);

        categoriesMock.Setup(r => r.UpdateAsync(category, ct)).Returns(Task.CompletedTask);
        uowMock.Setup(r => r.SaveChangesAsync(ct)).ReturnsAsync(1);

        var result = await controller.MarkUnfinished(new BotEventController.MarkUnfinishedUsers(ev.Id, 2), ct);

        Assert.That(result, Is.InstanceOf<NoContentResult>());
        categoriesMock.Verify(r => r.UpdateAsync(category, ct), Times.Once);
        uowMock.Verify(r => r.SaveChangesAsync(ct), Times.Once);
    }
    
    /*[Test]
    public async Task GetUserEventsInfo_UserNotFound_ReturnsNotFound()
    {
        var ct = CancellationToken.None;
        usersMock.Setup(r => r.GetByTelegramIdAsync(1001, ct)).ReturnsAsync((User)null);

        var result = await controller.GetUserEventsInfo(1001, ct);

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        var nf = (NotFoundObjectResult)result.Result!;
        Assert.That(nf.Value, Is.EqualTo("User with TelegramId 1001 not found"));
    }

    [Test]
    public async Task GetUserEventsInfo_UserFound_ReturnsOnlyEventsWhereUserParticipates()
    {
        var ct = CancellationToken.None;

        var user = new User(1001, "U", "u", new List<string> { "G1", "SG1" });

        var g = new Group("G1");
        var sg = new Group("SG1");
        

        var ev1 = new Event(Guid.NewGuid(), DateTimeOffset.UtcNow, "G1");
        ev1.ParticipantsTelegramIds.AddRange(new[] { 1001L, 777L });

        var ev2 = new Event(Guid.NewGuid(), DateTimeOffset.UtcNow, "G1");
        ev2.ParticipantsTelegramIds.AddRange(new[] { 777L });

        var id3 = Guid.NewGuid();
        
        g.AddEvent(ev1.Id);
        g.AddEvent(ev2.Id);
        sg.AddEvent(id3);

        usersMock.Setup(r => r.GetByTelegramIdAsync(1001, ct)).ReturnsAsync(user);
        groupsMock.Setup(r => r.GetByCodeAsync("G1", ct)).ReturnsAsync(g);
        groupsMock.Setup(r => r.GetByCodeAsync("SG1", ct)).ReturnsAsync(sg);

        eventsMock.Setup(r => r.GetByIdAsync(ev1.Id, ct)).ReturnsAsync(ev1);
        eventsMock.Setup(r => r.GetByIdAsync(ev2.Id, ct)).ReturnsAsync(ev2);
        eventsMock.Setup(r => r.GetByIdAsync(id3, ct)).ReturnsAsync((Event)null);

        categoriesMock.Setup(r => r.GetByIdAsync(ev1.CategoryId, ct)).ReturnsAsync(new EventCategory("Sport", false, "G1"));

        var result = await controller.GetUserEventsInfo(1001, ct);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var dtos = (List<BotEventController.BotEventDto>)ok.Value!;

        Assert.That(dtos.Count, Is.EqualTo(1));
        Assert.That(dtos[0].EventId, Is.EqualTo(ev1.Id));
    }*/
    
    [Test]
    public async Task GetUserEventsInfo_UserNotFound_ReturnsNotFound()
    {
        var ct = CancellationToken.None;
        var telegramId = 1001L;

        usersMock
            .Setup(r => r.GetByTelegramIdAsync(telegramId, ct))
            .ReturnsAsync((User)null);

        var result = await controller.GetUserEventsInfo(telegramId, ct);

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
        var notFound = (NotFoundObjectResult)result.Result!;
        Assert.That(notFound.Value, Is.EqualTo($"User with TelegramId {telegramId} not found"));

        groupsMock.Verify(r => r.GetByCodeAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Never);
        eventsMock.Verify(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Test]
    public async Task GetUserEventsInfo_UserFound_FiltersOnlyParticipatingEvents_ReturnsOkDtos()
    {
        var ct = CancellationToken.None;
        var telegramId = 1001L;

        var user = new User(telegramId, "U", "u", new List<string> { "G1", "SG1" });

        var group = new Group("G1");

        var subGroup = new Group("SG1");
        
        var id = Guid.NewGuid();

        var ev1 = new Event(category: id, occurredOn: DateTimeOffset.UtcNow, groupCode: "G1");
        ev1.ParticipantsTelegramIds.AddRange(new[] { telegramId, 777L });

        var ev2 = new Event(category: id, occurredOn: DateTimeOffset.UtcNow, groupCode: "G1");
        ev2.ParticipantsTelegramIds.AddRange(new[] { 777L });

        group.EventsIds.Add(ev1.Id);
        group.EventsIds.Add(ev2.Id);

        var ev3 = new Event(category: id, occurredOn: DateTimeOffset.UtcNow, groupCode: "SG1");
        ev3.ParticipantsTelegramIds.AddRange(new[] { 999L });

        subGroup.EventsIds.Add(ev3.Id);

        usersMock.Setup(r => r.GetByTelegramIdAsync(telegramId, ct)).ReturnsAsync(user);

        groupsMock.Setup(r => r.GetByCodeAsync("G1", ct)).ReturnsAsync(group);
        groupsMock.Setup(r => r.GetByCodeAsync("SG1", ct)).ReturnsAsync(subGroup);

        eventsMock
            .Setup(r => r.GetByIdsAsync(
                It.Is<List<Guid>>(ids =>
                    ids.Count == 3 &&
                    ids.Contains(ev1.Id) &&
                    ids.Contains(ev2.Id) &&
                    ids.Contains(ev3.Id)),
                ct))
            .ReturnsAsync(new List<Event> { ev1, ev2, ev3 });

        categoriesMock
            .Setup(r => r.GetByIdAsync(id, ct))
            .ReturnsAsync(new EventCategory("CP", false, "G1"));

        var result = await controller.GetUserEventsInfo(telegramId, ct);

        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
        var ok = (OkObjectResult)result.Result!;
        var dtos = (List<BotEventController.BotEventDto>)ok.Value!;

        Assert.That(dtos.Count, Is.EqualTo(1));
        Assert.That(dtos[0].EventId, Is.EqualTo(ev1.Id));
        Assert.That(dtos[0].TelegramId, Is.EquivalentTo(new[] { telegramId, 777L }));

        groupsMock.Verify(r => r.GetByCodeAsync("G1", ct), Times.Once);
        groupsMock.Verify(r => r.GetByCodeAsync("SG1", ct), Times.Once);
        eventsMock.Verify(r => r.GetByIdsAsync(It.IsAny<List<Guid>>(), ct), Times.Once);
    }
}