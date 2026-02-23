using FluentAssertions;
using soat.eleven.kutcut.domain.Entities;
using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.tests.Domain;

public class NotificationEntityTests
{
    [Fact]
    public void Notification_CanSetAndGetAllProperties()
    {
        var id = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var now = DateTime.UtcNow;

        var notification = new Notification
        {
            Id = id,
            UserId = userId,
            VideoId = videoId,
            Type = NotificationTypeEnum.ProcessadoComSucesso,
            Description = "test",
            CreatedAt = now
        };

        notification.Id.Should().Be(id);
        notification.UserId.Should().Be(userId);
        notification.VideoId.Should().Be(videoId);
        notification.Type.Should().Be(NotificationTypeEnum.ProcessadoComSucesso);
        notification.Description.Should().Be("test");
        notification.CreatedAt.Should().Be(now);
    }

    [Fact]
    public void Notification_NullableFields_AcceptNull()
    {
        var notification = new Notification
        {
            Id = Guid.NewGuid(),
            UserId = null,
            VideoId = null,
            Type = null,
            Description = null,
            CreatedAt = null
        };

        notification.UserId.Should().BeNull();
        notification.VideoId.Should().BeNull();
        notification.Type.Should().BeNull();
        notification.Description.Should().BeNull();
        notification.CreatedAt.Should().BeNull();
    }

    [Theory]
    [InlineData(NotificationTypeEnum.ProcessadoComSucesso)]
    [InlineData(NotificationTypeEnum.ProcessadoComErro)]
    public void Notification_AllNotificationTypeValues_CanBeAssigned(NotificationTypeEnum type)
    {
        var notification = new Notification { Id = Guid.NewGuid(), Type = type };

        notification.Type.Should().Be(type);
    }
}

public class NotificationTypeEntityTests
{
    [Fact]
    public void NotificationType_CanSetAndGetAllProperties()
    {
        var entity = new NotificationType
        {
            Id = NotificationTypeEnum.ProcessadoComErro,
            Description = "Processado com erro"
        };

        entity.Id.Should().Be(NotificationTypeEnum.ProcessadoComErro);
        entity.Description.Should().Be("Processado com erro");
    }

    [Fact]
    public void NotificationType_DescriptionCanBeNull()
    {
        var entity = new NotificationType { Id = NotificationTypeEnum.ProcessadoComSucesso, Description = null };

        entity.Description.Should().BeNull();
    }
}

public class StatusEntityTests
{
    [Fact]
    public void Status_CanSetAndGetAllProperties()
    {
        var entity = new Status
        {
            Id = StatusEnum.EmProcessamento,
            Description = "Em processamento"
        };

        entity.Id.Should().Be(StatusEnum.EmProcessamento);
        entity.Description.Should().Be("Em processamento");
    }

    [Theory]
    [InlineData(StatusEnum.Pendente)]
    [InlineData(StatusEnum.Uploaded)]
    [InlineData(StatusEnum.EmProcessamento)]
    [InlineData(StatusEnum.ProcessadoComSucesso)]
    [InlineData(StatusEnum.ProcessadoComErro)]
    public void Status_AllStatusEnumValues_CanBeAssigned(StatusEnum status)
    {
        var entity = new Status { Id = status };

        entity.Id.Should().Be(status);
    }
}
