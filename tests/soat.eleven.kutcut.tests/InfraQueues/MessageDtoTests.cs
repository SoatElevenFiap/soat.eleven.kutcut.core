using FluentAssertions;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.infra.queues.MessagesDtos;

namespace soat.eleven.kutcut.tests.InfraQueues;

public class MessageDtoTests
{
    // ── VideoUploadedMessage ──────────────────────────────────────────────────

    [Fact]
    public void VideoUploadedMessage_DefaultValues_AreCorrect()
    {
        var msg = new VideoUploadedMessage();

        msg.UserId.Should().Be(Guid.Empty);
        msg.Filename.Should().BeEmpty();
        msg.Title.Should().BeEmpty();
        msg.MessageId.Should().Be(Guid.Empty);
        msg.Status.Should().Be(0);
    }

    [Fact]
    public void VideoUploadedMessage_SetProperties_RetainValues()
    {
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var msg = new VideoUploadedMessage
        {
            UserId = userId,
            Filename = "video.mp4",
            Title = "My Video",
            MessageId = messageId,
            Status = 1
        };

        msg.UserId.Should().Be(userId);
        msg.Filename.Should().Be("video.mp4");
        msg.Title.Should().Be("My Video");
        msg.MessageId.Should().Be(messageId);
        msg.Status.Should().Be(1);
    }

    // ── VideoProcessingMessage ────────────────────────────────────────────────

    [Fact]
    public void VideoProcessingMessage_DefaultValues_AreCorrect()
    {
        var msg = new VideoProcessingMessage();

        msg.UserId.Should().Be(Guid.Empty);
        msg.Filename.Should().BeEmpty();
        msg.Title.Should().BeEmpty();
        msg.MessageId.Should().Be(Guid.Empty);
        msg.Status.Should().Be(default(StatusEnum));
    }

    [Fact]
    public void VideoProcessingMessage_SetProperties_RetainValues()
    {
        var userId = Guid.NewGuid();
        var messageId = Guid.NewGuid();

        var msg = new VideoProcessingMessage
        {
            UserId = userId,
            Filename = "clip.mp4",
            Title = "Processing Video",
            MessageId = messageId,
            Status = StatusEnum.ProcessadoComSucesso
        };

        msg.UserId.Should().Be(userId);
        msg.Filename.Should().Be("clip.mp4");
        msg.Title.Should().Be("Processing Video");
        msg.MessageId.Should().Be(messageId);
        msg.Status.Should().Be(StatusEnum.ProcessadoComSucesso);
    }

    [Theory]
    [InlineData(StatusEnum.Pendente)]
    [InlineData(StatusEnum.Uploaded)]
    [InlineData(StatusEnum.EmProcessamento)]
    [InlineData(StatusEnum.ProcessadoComSucesso)]
    [InlineData(StatusEnum.ProcessadoComErro)]
    public void VideoProcessingMessage_AllStatusValues_CanBeAssigned(StatusEnum status)
    {
        var msg = new VideoProcessingMessage { Status = status };

        msg.Status.Should().Be(status);
    }
}
