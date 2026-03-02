using FluentAssertions;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.Models.Enums;

namespace soat.eleven.kutcut.tests.Infra;

public class InfraModelsTests
{
    // ── VideoModel ────────────────────────────────────────────────────────────

    [Fact]
    public void VideoModel_SetProperties_RetainValues()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var video = new VideoModel
        {
            Id = id,
            Title = "My Video",
            UserId = userId,
            Filename = "video.mp4",
            Status = StatusEnum.Pendente,
            CreatedAt = now,
            UpdatedAt = now
        };

        video.Id.Should().Be(id);
        video.Title.Should().Be("My Video");
        video.UserId.Should().Be(userId);
        video.Filename.Should().Be("video.mp4");
        video.Status.Should().Be(StatusEnum.Pendente);
        video.CreatedAt.Should().Be(now);
        video.UpdatedAt.Should().Be(now);
    }

    [Fact]
    public void VideoModel_DefaultNotificationsCollection_IsEmpty()
    {
        var video = new VideoModel { Filename = "file.mp4" };

        video.Notifications.Should().NotBeNull();
        video.Notifications.Should().BeEmpty();
    }

    [Fact]
    public void VideoModel_StatusNavigation_CanBeAssigned()
    {
        var status = new StatusModel { Id = StatusEnum.Uploaded, Description = "Uploaded" };
        var video = new VideoModel { Filename = "file.mp4", StatusNavigation = status };

        video.StatusNavigation.Should().BeSameAs(status);
    }

    // ── StatusModel ───────────────────────────────────────────────────────────

    [Fact]
    public void StatusModel_SetProperties_RetainValues()
    {
        var status = new StatusModel
        {
            Id = StatusEnum.ProcessadoComSucesso,
            Description = "Processado com sucesso"
        };

        status.Id.Should().Be(StatusEnum.ProcessadoComSucesso);
        status.Description.Should().Be("Processado com sucesso");
    }

    [Fact]
    public void StatusModel_DefaultVideosCollection_IsEmpty()
    {
        var status = new StatusModel();

        status.Videos.Should().NotBeNull();
        status.Videos.Should().BeEmpty();
    }

    // ── NotificationModel ─────────────────────────────────────────────────────

    [Fact]
    public void NotificationModel_SetProperties_RetainValues()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var notification = new NotificationModel
        {
            Id = id,
            UserId = userId,
            VideoId = videoId,
            Type = NotificationTypeEnum.ProcessadoComSucesso,
            Description = "Processing done",
            CreatedAt = now
        };

        notification.Id.Should().Be(id);
        notification.UserId.Should().Be(userId);
        notification.VideoId.Should().Be(videoId);
        notification.Type.Should().Be(NotificationTypeEnum.ProcessadoComSucesso);
        notification.Description.Should().Be("Processing done");
        notification.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void NotificationModel_NavigationProperties_AreNullByDefault()
    {
        var notification = new NotificationModel();

        notification.Video.Should().BeNull();
        notification.TypeNavigation.Should().BeNull();
    }

    // ── NotificationTypeModel ─────────────────────────────────────────────────

    [Fact]
    public void NotificationTypeModel_SetProperties_RetainValues()
    {
        var model = new NotificationTypeModel
        {
            Id = NotificationTypeEnum.ProcessadoComErro,
            Description = "Processado com erro"
        };

        model.Id.Should().Be(NotificationTypeEnum.ProcessadoComErro);
        model.Description.Should().Be("Processado com erro");
    }

    [Fact]
    public void NotificationTypeModel_DefaultNotificationsCollection_IsEmpty()
    {
        var model = new NotificationTypeModel();

        model.Notifications.Should().NotBeNull();
        model.Notifications.Should().BeEmpty();
    }

    // ── StatusEnum ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(StatusEnum.Pendente, 1)]
    [InlineData(StatusEnum.Uploaded, 2)]
    [InlineData(StatusEnum.EmProcessamento, 3)]
    [InlineData(StatusEnum.ProcessadoComSucesso, 4)]
    [InlineData(StatusEnum.ProcessadoComErro, 5)]
    public void StatusEnum_Values_AreCorrect(StatusEnum value, int expected)
    {
        ((int)value).Should().Be(expected);
    }

    // ── NotificationTypeEnum ──────────────────────────────────────────────────

    [Theory]
    [InlineData(NotificationTypeEnum.ProcessadoComSucesso, 1)]
    [InlineData(NotificationTypeEnum.ProcessadoComErro, 2)]
    public void NotificationTypeEnum_Values_AreCorrect(NotificationTypeEnum value, int expected)
    {
        ((int)value).Should().Be(expected);
    }
}
