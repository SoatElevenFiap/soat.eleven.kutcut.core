using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.application.Processors;
using soat.eleven.kutcut.domain.Dtos;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.domain.Notifications;
using soat.eleven.kutcut.domain.Services;
using soat.eleven.kutcut.infra.queues.MessagesDtos;

namespace soat.eleven.kutcut.tests.Application;

public class VideoNotificationProcessorTests
{
    private readonly Mock<ILogger<VideoNotificationProcessor>> _logger;
    private readonly Mock<IUserSerivce> _userService;
    private readonly Mock<IUserNotificaton> _userNotification;
    private readonly Mock<IVideoMessageFactory> _messageFactory;
    private readonly VideoNotificationProcessor _sut;

    public VideoNotificationProcessorTests()
    {
        _logger = new Mock<ILogger<VideoNotificationProcessor>>();
        _userService = new Mock<IUserSerivce>();
        _userNotification = new Mock<IUserNotificaton>();
        _messageFactory = new Mock<IVideoMessageFactory>();

        _sut = new VideoNotificationProcessor(
            _logger.Object,
            _userService.Object,
            _userNotification.Object,
            _messageFactory.Object);
    }

    private static VideoProcessingMessage BuildMessage(StatusEnum status = StatusEnum.ProcessadoComSucesso)
        => new()
        {
            UserId = Guid.NewGuid(),
            Filename = "video.mp4",
            Title = "Meu Vídeo",
            MessageId = Guid.NewGuid(),
            Status = status
        };

    [Fact]
    public async Task ProcessVideoNotificationAsync_ValidMessage_NotifiesUser()
    {
        var message = BuildMessage();
        var user = new UserDto { Name = "João", Email = "joao@email.com" };
        var notify = new NotifyMessage { Title = "Success", Body = "Done" };

        _userService.Setup(x => x.GetUser(message.UserId)).Returns(user);
        _messageFactory.Setup(x => x.BuildNotificationMessage(message)).Returns(notify);
        _userNotification.Setup(x => x.NotifyUser(user, notify)).Returns(true);

        await _sut.ProcessVideoNotificationAsync(message);

        _userNotification.Verify(x => x.NotifyUser(user, notify), Times.Once);
    }

    [Fact]
    public async Task ProcessVideoNotificationAsync_NullMessage_LogsErrorAndDoesNotNotify()
    {
        await _sut.ProcessVideoNotificationAsync(null!);

        _userNotification.Verify(
            x => x.NotifyUser(It.IsAny<UserDto>(), It.IsAny<NotifyMessage>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessVideoNotificationAsync_UserNotFound_DoesNotNotify()
    {
        var message = BuildMessage();
        _userService.Setup(x => x.GetUser(message.UserId)).Returns((UserDto)null!);

        await _sut.ProcessVideoNotificationAsync(message);

        _userNotification.Verify(
            x => x.NotifyUser(It.IsAny<UserDto>(), It.IsAny<NotifyMessage>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessVideoNotificationAsync_UnknownStatus_DoesNotNotify()
    {
        var message = BuildMessage(StatusEnum.Pendente);
        var user = new UserDto { Name = "João", Email = "joao@email.com" };

        _userService.Setup(x => x.GetUser(message.UserId)).Returns(user);
        _messageFactory.Setup(x => x.BuildNotificationMessage(message)).Returns((NotifyMessage?)null);

        await _sut.ProcessVideoNotificationAsync(message);

        _userNotification.Verify(
            x => x.NotifyUser(It.IsAny<UserDto>(), It.IsAny<NotifyMessage>()),
            Times.Never);
    }

    [Fact]
    public async Task ProcessVideoNotificationAsync_UserServiceThrows_RethrowsException()
    {
        var message = BuildMessage();
        _userService.Setup(x => x.GetUser(message.UserId))
                    .Throws(new InvalidOperationException("service error"));

        Func<Task> act = () => _sut.ProcessVideoNotificationAsync(message);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("service error");
    }

    [Fact]
    public async Task ProcessVideoNotificationAsync_NotificationServiceReturnsFalse_DoesNotThrow()
    {
        var message = BuildMessage();
        var user = new UserDto { Name = "João", Email = "joao@email.com" };
        var notify = new NotifyMessage { Title = "T", Body = "B" };

        _userService.Setup(x => x.GetUser(message.UserId)).Returns(user);
        _messageFactory.Setup(x => x.BuildNotificationMessage(message)).Returns(notify);
        _userNotification.Setup(x => x.NotifyUser(user, notify)).Returns(false);

        Func<Task> act = () => _sut.ProcessVideoNotificationAsync(message);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ProcessVideoNotificationAsync_CallsGetUserWithCorrectId()
    {
        var message = BuildMessage();
        var user = new UserDto { Name = "X", Email = "x@x.com" };
        var notify = new NotifyMessage { Title = "T", Body = "B" };

        _userService.Setup(x => x.GetUser(message.UserId)).Returns(user);
        _messageFactory.Setup(x => x.BuildNotificationMessage(message)).Returns(notify);
        _userNotification.Setup(x => x.NotifyUser(user, notify)).Returns(true);

        await _sut.ProcessVideoNotificationAsync(message);

        _userService.Verify(x => x.GetUser(message.UserId), Times.Once);
    }
}
