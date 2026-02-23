using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.application.NotificationService;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.queues.Interfaces;
using soat.eleven.kutcut.infra.queues.MessagesDtos;
using System.Text.Json;

namespace soat.eleven.kutcut.tests.Application;

/// <summary>
/// Testes de recepcao de mensagens da fila e processamento no BackgroundNotificationService.
/// Captura o callback registrado no StartListening para simular mensagens chegando da fila.
/// </summary>
public class BackgroundNotificationServiceProcessingTests
{
    private readonly Mock<ILogger<BackgroundNotificationService>> _logger;
    private readonly Mock<IMessageListener> _messageListener;
    private readonly Mock<IVideoNotificationProcessor> _processor;
    private readonly Mock<IVideoMessageFactory> _messageFactory;
    private readonly Mock<IServiceScopeFactory> _scopeFactory;
    private readonly Mock<IServiceScope> _scope;
    private readonly Mock<IServiceProvider> _serviceProvider;
    private readonly Mock<IVideoService> _videoService;
    private readonly BackgroundNotificationService _sut;

    private Func<string, Task>? _capturedCallback;

    public BackgroundNotificationServiceProcessingTests()
    {
        _logger = new Mock<ILogger<BackgroundNotificationService>>();
        _messageListener = new Mock<IMessageListener>();
        _processor = new Mock<IVideoNotificationProcessor>();
        _messageFactory = new Mock<IVideoMessageFactory>();
        _scopeFactory = new Mock<IServiceScopeFactory>();
        _scope = new Mock<IServiceScope>();
        _serviceProvider = new Mock<IServiceProvider>();
        _videoService = new Mock<IVideoService>();

        _scope.Setup(x => x.ServiceProvider).Returns(_serviceProvider.Object);
        _scopeFactory.Setup(x => x.CreateScope()).Returns(_scope.Object);
        _serviceProvider
            .Setup(x => x.GetService(typeof(IVideoService)))
            .Returns(_videoService.Object);

        // Capturar o callback registrado no StartListening
        _messageListener
            .Setup(x => x.StartListening(It.IsAny<string>(), It.IsAny<Func<string, Task>>()))
            .Callback<string, Func<string, Task>>((_, cb) => _capturedCallback = cb);

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

    /// <summary>
    /// Inicia o servico e aguarda o callback ser registrado pelo background task.
    /// Necessario porque StartListening e chamado dentro de Task.Run.
    /// </summary>
    private async Task StartAndWaitForCallbackAsync()
    {
        using var cts = new CancellationTokenSource();
        cts.CancelAfter(500);
        try { await _sut.StartAsync(cts.Token); } catch { /* cancellation esperado */ }

        var deadline = DateTime.UtcNow.AddMilliseconds(500);
        while (_capturedCallback == null && DateTime.UtcNow < deadline)
            await Task.Delay(10);
    }

    private string SerializeMessage(VideoProcessingMessage msg)
        => JsonSerializer.Serialize(msg);

    private VideoProcessingMessage BuildProcessingMessage(
        StatusEnum status = StatusEnum.ProcessadoComSucesso,
        Guid? messageId = null)
        => new()
        {
            UserId = Guid.NewGuid(),
            Filename = "video.mp4",
            Title = "Titulo do Video",
            MessageId = messageId ?? Guid.NewGuid(),
            Status = status
        };

    private soat.eleven.kutcut.application.Dtos.Video.VideoResult BuildVideoResult(
        VideoProcessingMessage msg)
        => new(msg.MessageId, "Titulo", msg.UserId, msg.Filename,
               msg.Status, msg.Status.ToString(), DateTime.UtcNow, null);

    // ── StopAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task StopAsync_CallsStopListeningOnMessageListener()
    {
        await _sut.StopAsync(CancellationToken.None);

        _messageListener.Verify(x => x.StopListening(), Times.Once);
    }

    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        Func<Task> act = () => _sut.StopAsync(CancellationToken.None);

        await act.Should().NotThrowAsync();
    }

    // ── ExecuteAsync: registro na fila correta ─────────────────────────────────

    [Fact]
    public async Task ExecuteAsync_RegistersListenerOnCorrectQueue()
    {
        await StartAndWaitForCallbackAsync();

        _messageListener.Verify(
            x => x.StartListening("processamento_de_videos", It.IsAny<Func<string, Task>>()),
            Times.Once);
    }

    // ── ProcessVideoMessage: mensagem valida ──────────────────────────────────

    [Fact]
    public async Task ProcessVideoMessage_ValidMessage_UpdatesVideoStatus()
    {
        var msg = BuildProcessingMessage(StatusEnum.ProcessadoComSucesso);
        var json = SerializeMessage(msg);

        _messageFactory.Setup(x => x.DeserializeVideoMessage(json)).Returns(msg);
        _videoService
            .Setup(x => x.UpdateStatusAsync(msg.MessageId, msg.Status))
            .ReturnsAsync(FluentResults.Result.Ok(BuildVideoResult(msg)));

        await StartAndWaitForCallbackAsync();

        _capturedCallback.Should().NotBeNull("o listener deveria ter sido registrado");
        await _capturedCallback!(json);

        _videoService.Verify(x => x.UpdateStatusAsync(msg.MessageId, msg.Status), Times.Once);
    }

    [Fact]
    public async Task ProcessVideoMessage_ValidMessageAndStatusUpdateSuccess_NotifiesUser()
    {
        var msg = BuildProcessingMessage(StatusEnum.ProcessadoComSucesso);
        var json = SerializeMessage(msg);

        _messageFactory.Setup(x => x.DeserializeVideoMessage(json)).Returns(msg);
        _videoService
            .Setup(x => x.UpdateStatusAsync(msg.MessageId, msg.Status))
            .ReturnsAsync(FluentResults.Result.Ok(BuildVideoResult(msg)));
        _processor
            .Setup(x => x.ProcessVideoNotificationAsync(msg))
            .Returns(Task.CompletedTask);

        await StartAndWaitForCallbackAsync();
        await _capturedCallback!(json);

        _processor.Verify(x => x.ProcessVideoNotificationAsync(msg), Times.Once);
    }

    [Fact]
    public async Task ProcessVideoMessage_ProcessadoComErro_UpdatesStatusAndNotifies()
    {
        var msg = BuildProcessingMessage(StatusEnum.ProcessadoComErro);
        var json = SerializeMessage(msg);

        _messageFactory.Setup(x => x.DeserializeVideoMessage(json)).Returns(msg);
        _videoService
            .Setup(x => x.UpdateStatusAsync(msg.MessageId, msg.Status))
            .ReturnsAsync(FluentResults.Result.Ok(BuildVideoResult(msg)));

        await StartAndWaitForCallbackAsync();
        await _capturedCallback!(json);

        _videoService.Verify(x => x.UpdateStatusAsync(msg.MessageId, StatusEnum.ProcessadoComErro), Times.Once);
    }

    // ── ProcessVideoMessage: mensagem invalida (null apos deserializacao) ──────

    [Fact]
    public async Task ProcessVideoMessage_NullMessage_ThrowsException()
    {
        var json = "mensagem-invalida";
        _messageFactory.Setup(x => x.DeserializeVideoMessage(json)).Returns((VideoProcessingMessage?)null);

        await StartAndWaitForCallbackAsync();

        Func<Task> act = () => _capturedCallback!(json);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ProcessVideoMessage_NullMessage_DoesNotCallVideoService()
    {
        var json = "invalido";
        _messageFactory.Setup(x => x.DeserializeVideoMessage(json)).Returns((VideoProcessingMessage?)null);

        await StartAndWaitForCallbackAsync();
        try { await _capturedCallback!(json); } catch { }

        _videoService.Verify(
            x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<StatusEnum>()),
            Times.Never);
    }

    // ── ProcessVideoMessage: falha ao atualizar status ───────────────────────

    [Fact]
    public async Task ProcessVideoMessage_UpdateStatusFails_ThrowsException()
    {
        var msg = BuildProcessingMessage();
        var json = SerializeMessage(msg);

        _messageFactory.Setup(x => x.DeserializeVideoMessage(json)).Returns(msg);
        _videoService
            .Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<StatusEnum>()))
            .ReturnsAsync(FluentResults.Result.Fail<soat.eleven.kutcut.application.Dtos.Video.VideoResult>("Video nao encontrado"));

        await StartAndWaitForCallbackAsync();

        Func<Task> act = () => _capturedCallback!(json);

        await act.Should().ThrowAsync<Exception>();
    }

    [Fact]
    public async Task ProcessVideoMessage_UpdateStatusFails_DoesNotNotify()
    {
        var msg = BuildProcessingMessage();
        var json = SerializeMessage(msg);

        _messageFactory.Setup(x => x.DeserializeVideoMessage(json)).Returns(msg);
        _videoService
            .Setup(x => x.UpdateStatusAsync(It.IsAny<Guid>(), It.IsAny<StatusEnum>()))
            .ReturnsAsync(FluentResults.Result.Fail<soat.eleven.kutcut.application.Dtos.Video.VideoResult>("Video nao encontrado"));

        await StartAndWaitForCallbackAsync();
        try { await _capturedCallback!(json); } catch { }

        _processor.Verify(
            x => x.ProcessVideoNotificationAsync(It.IsAny<VideoProcessingMessage>()),
            Times.Never);
    }

    // ── ProcessVideoMessage: statuses variaveis ───────────────────────────────

    [Theory]
    [InlineData(StatusEnum.ProcessadoComSucesso)]
    [InlineData(StatusEnum.ProcessadoComErro)]
    [InlineData(StatusEnum.EmProcessamento)]
    public async Task ProcessVideoMessage_AnyStatus_CallsUpdateStatus(StatusEnum status)
    {
        var msg = BuildProcessingMessage(status);
        var json = SerializeMessage(msg);

        _messageFactory.Setup(x => x.DeserializeVideoMessage(json)).Returns(msg);
        _videoService
            .Setup(x => x.UpdateStatusAsync(msg.MessageId, status))
            .ReturnsAsync(FluentResults.Result.Ok(BuildVideoResult(msg)));

        await StartAndWaitForCallbackAsync();
        await _capturedCallback!(json);

        _videoService.Verify(x => x.UpdateStatusAsync(msg.MessageId, status), Times.Once);
    }
}
