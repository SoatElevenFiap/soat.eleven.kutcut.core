using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.application.NotificationService;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.queues.Interfaces;

namespace soat.eleven.kutcut.tests.Application;

public class BackgroundNotificationServiceTests
{
    private readonly Mock<ILogger<BackgroundNotificationService>> _logger;
    private readonly Mock<IMessageListener> _messageListener;
    private readonly Mock<IVideoNotificationProcessor> _processor;
    private readonly Mock<IVideoMessageFactory> _messageFactory;
    private readonly Mock<IServiceScopeFactory> _scopeFactory;
    private readonly BackgroundNotificationService _sut;

    public BackgroundNotificationServiceTests()
    {
        _logger = new Mock<ILogger<BackgroundNotificationService>>();
        _messageListener = new Mock<IMessageListener>();
        _processor = new Mock<IVideoNotificationProcessor>();
        _messageFactory = new Mock<IVideoMessageFactory>();
        _scopeFactory = new Mock<IServiceScopeFactory>();

        var settings = Options.Create(new RabbitMQSettings
        {
            VideoProcessingQueueName = "processamento_de_videos"
        });

        _sut = new BackgroundNotificationService(
            _logger.Object,
            _messageListener.Object,
            _processor.Object,
            settings,
            _messageFactory.Object,
            _scopeFactory.Object);
    }

    [Fact]
    public async Task StopAsync_CallsStopListeningOnMessageListener()
    {
        using var cts = new CancellationTokenSource();

        await _sut.StopAsync(cts.Token);

        _messageListener.Verify(x => x.StopListening(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        using var cts = new CancellationTokenSource();

        Func<Task> act = () => _sut.StopAsync(cts.Token);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_WhenCancelledImmediately_CompletesGracefully()
    {
        using var cts = new CancellationTokenSource();

        // Configure StartListening to not block
        _messageListener.Setup(x => x.StartListening(
            It.IsAny<string>(),
            It.IsAny<Func<string, Task>>()));

        // Cancel immediately after starting
        cts.Cancel();

        Func<Task> act = async () =>
        {
            await _sut.StartAsync(cts.Token);
            await _sut.StopAsync(cts.Token);
        };

        await act.Should().NotThrowAsync();
    }
}
