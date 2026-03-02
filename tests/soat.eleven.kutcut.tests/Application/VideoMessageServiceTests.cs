using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using soat.eleven.kutcut.application.Services;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.infra.queues.MessagesDtos;

namespace soat.eleven.kutcut.tests.Application;

public class VideoMessageServiceTests
{
    private readonly VideoMessageService _sut;

    public VideoMessageServiceTests()
    {
        var logger = new Mock<ILogger<VideoMessageService>>();
        _sut = new VideoMessageService(logger.Object);
    }

    // ── DeserializeVideoMessage ──────────────────────────────────────────────

    [Fact]
    public void DeserializeVideoMessage_ValidJson_ReturnsMessage()
    {
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid();
        var json = $$"""
        {
            "userId": "{{userId}}",
            "filename": "video.mp4",
            "title": "Meu Vídeo",
            "messageId": "{{messageId}}",
            "status": 4
        }
        """;

        var result = _sut.DeserializeVideoMessage(json);

        result.Should().NotBeNull();
        result!.UserId.Should().Be(userId);
        result.Filename.Should().Be("video.mp4");
        result.Title.Should().Be("Meu Vídeo");
        result.MessageId.Should().Be(messageId);
        result.Status.Should().Be(StatusEnum.ProcessadoComSucesso);
    }

    [Fact]
    public void DeserializeVideoMessage_InvalidJson_ReturnsNull()
    {
        var result = _sut.DeserializeVideoMessage("invalid-json{{{");

        result.Should().BeNull();
    }

    [Fact]
    public void DeserializeVideoMessage_EmptyJson_DoesNotThrow()
    {
        var act = () => _sut.DeserializeVideoMessage("{}");

        act.Should().NotThrow();
    }

    [Fact]
    public void DeserializeVideoMessage_IsCaseInsensitive()
    {
        var userId = Guid.NewGuid();
        var json = $$"""{"USERID": "{{userId}}", "FILENAME": "v.mp4", "TITLE": "t", "MESSAGEID": "{{Guid.NewGuid()}}", "STATUS": 1}""";

        var result = _sut.DeserializeVideoMessage(json);

        // PropertyNameCaseInsensitive = true, so this should map correctly
        result.Should().NotBeNull();
    }

    // ── BuildNotificationMessage ─────────────────────────────────────────────

    [Fact]
    public void BuildNotificationMessage_ProcessadoComSucesso_ReturnsSuccessMessage()
    {
        var videoMessage = new VideoProcessingMessage
        {
            Title = "Meu Vídeo",
            Status = StatusEnum.ProcessadoComSucesso,
            UserId = Guid.NewGuid(),
            MessageId = Guid.NewGuid()
        };

        var result = _sut.BuildNotificationMessage(videoMessage);

        result.Should().NotBeNull();
        result!.Title.Should().Contain("Successfully");
        result.Body.Should().Contain("Meu Vídeo");
    }

    [Fact]
    public void BuildNotificationMessage_ProcessadoComErro_ReturnsErrorMessage()
    {
        var videoMessage = new VideoProcessingMessage
        {
            Title = "Meu Vídeo",
            Status = StatusEnum.ProcessadoComErro,
            UserId = Guid.NewGuid(),
            MessageId = Guid.NewGuid()
        };

        var result = _sut.BuildNotificationMessage(videoMessage);

        result.Should().NotBeNull();
        result!.Title.Should().Contain("Failed");
        result.Body.Should().Contain("Meu Vídeo");
    }

    [Theory]
    [InlineData(StatusEnum.Pendente)]
    [InlineData(StatusEnum.Uploaded)]
    [InlineData(StatusEnum.EmProcessamento)]
    public void BuildNotificationMessage_NonTerminalStatus_ReturnsNull(StatusEnum status)
    {
        var videoMessage = new VideoProcessingMessage
        {
            Title = "Meu Vídeo",
            Status = status,
            UserId = Guid.NewGuid(),
            MessageId = Guid.NewGuid()
        };

        var result = _sut.BuildNotificationMessage(videoMessage);

        result.Should().BeNull();
    }

    [Fact]
    public void BuildNotificationMessage_SuccessBody_MentionsPlatformAvailability()
    {
        var msg = new VideoProcessingMessage
        {
            Title = "TestVideo",
            Status = StatusEnum.ProcessadoComSucesso,
            UserId = Guid.NewGuid(),
            MessageId = Guid.NewGuid()
        };

        var result = _sut.BuildNotificationMessage(msg);

        result!.Body.Should().Contain("platform");
    }

    [Fact]
    public void BuildNotificationMessage_ErrorBody_MentionsVerifyIssue()
    {
        var msg = new VideoProcessingMessage
        {
            Title = "TestVideo",
            Status = StatusEnum.ProcessadoComErro,
            UserId = Guid.NewGuid(),
            MessageId = Guid.NewGuid()
        };

        var result = _sut.BuildNotificationMessage(msg);

        result!.Body.Should().Contain("verify");
    }
}
