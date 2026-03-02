using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using soat.eleven.kutcut.application.Dtos.Video;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.application.Services;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.queues.Interfaces;
using soat.eleven.kutcut.infra.queues.MessagesDtos;
using soat.eleven.kutcut.infra.Repository;
using soat.eleven.kutcut.infra.Storage;

namespace soat.eleven.kutcut.tests.Application;

/// <summary>
/// Testes de envio de mensagem a fila apos upload de video.
/// </summary>
public class VideoServiceQueueTests
{
    private readonly Mock<IRepository<VideoModel>> _repository;
    private readonly Mock<IFileStorageService> _fileStorage;
    private readonly Mock<IUserContext> _userContext;
    private readonly Mock<IMessageSender> _messageSender;
    private readonly Mock<ILogger<VideoService>> _logger;
    private readonly VideoService _sut;
    private readonly Guid _userId = Guid.NewGuid();
    private const string VideoUploadedQueue = "video_uploaded";

    public VideoServiceQueueTests()
    {
        _repository  = new Mock<IRepository<VideoModel>>();
        _fileStorage = new Mock<IFileStorageService>();
        _userContext = new Mock<IUserContext>();
        _messageSender = new Mock<IMessageSender>();
        _logger = new Mock<ILogger<VideoService>>();

        var settings = Options.Create(new RabbitMQSettings
        {
            VideoUploadedQueueName = VideoUploadedQueue
        });

        _sut = new VideoService(
            _repository.Object,
            _fileStorage.Object,
            _userContext.Object,
            _messageSender.Object,
            settings,
            _logger.Object);

        _userContext.Setup(x => x.IsAuthenticated).Returns(true);
        _userContext.Setup(x => x.UserId).Returns(_userId);

        _fileStorage
            .Setup(x => x.SaveVideoAsync(
                It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync("https://storage.url/video.mp4");

        _repository
            .Setup(x => x.AddAsync(It.IsAny<VideoModel>()))
            .ReturnsAsync((VideoModel m) => m);

        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
    }

    // ── Envio para a fila correta ─────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_PublishesToCorrectQueueName()
    {
        await _sut.CreateAsync(new CreateVideoInput("Titulo", "video.mp4", new MemoryStream()));

        _messageSender.Verify(
            x => x.SendMessage(VideoUploadedQueue, It.IsAny<VideoUploadedMessage>()),
            Times.Once);
    }

    [Fact]
    public async Task CreateAsync_PublishesExactlyOneMessage()
    {
        await _sut.CreateAsync(new CreateVideoInput("Titulo", "video.mp4", new MemoryStream()));

        _messageSender.Verify(
            x => x.SendMessage(It.IsAny<string>(), It.IsAny<VideoUploadedMessage>()),
            Times.Once);
    }

    // ── Conteudo da VideoUploadedMessage ──────────────────────────────────────

    [Fact]
    public async Task CreateAsync_Message_ContainsCorrectUserId()
    {
        VideoUploadedMessage? captured = null;
        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<VideoUploadedMessage>()))
            .Callback<string, VideoUploadedMessage>((_, msg) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateVideoInput("Titulo", "video.mp4", new MemoryStream()));

        captured.Should().NotBeNull();
        captured!.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task CreateAsync_Message_ContainsCorrectFilename()
    {
        VideoUploadedMessage? captured = null;
        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<VideoUploadedMessage>()))
            .Callback<string, VideoUploadedMessage>((_, msg) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateVideoInput("Titulo", "meu_filme.mp4", new MemoryStream()));

        captured!.Filename.Should().Be("meu_filme.mp4");
    }

    [Fact]
    public async Task CreateAsync_Message_ContainsCorrectTitle()
    {
        VideoUploadedMessage? captured = null;
        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<VideoUploadedMessage>()))
            .Callback<string, VideoUploadedMessage>((_, msg) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateVideoInput("Video de Aula", "video.mp4", new MemoryStream()));

        captured!.Title.Should().Be("Video de Aula");
    }

    [Fact]
    public async Task CreateAsync_Message_StatusIsPendente()
    {
        VideoUploadedMessage? captured = null;
        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<VideoUploadedMessage>()))
            .Callback<string, VideoUploadedMessage>((_, msg) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateVideoInput("Titulo", "video.mp4", new MemoryStream()));

        captured!.Status.Should().Be((int)StatusEnum.Pendente);
    }

    [Fact]
    public async Task CreateAsync_Message_HasNonEmptyMessageId()
    {
        VideoUploadedMessage? captured = null;
        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<VideoUploadedMessage>()))
            .Callback<string, VideoUploadedMessage>((_, msg) => captured = msg)
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateVideoInput("Titulo", "video.mp4", new MemoryStream()));

        captured!.MessageId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public async Task CreateAsync_TwoUploads_GenerateDistinctMessageIds()
    {
        var ids = new List<Guid>();
        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<VideoUploadedMessage>()))
            .Callback<string, VideoUploadedMessage>((_, msg) => ids.Add(msg.MessageId))
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateVideoInput("Titulo1", "video1.mp4", new MemoryStream()));
        await _sut.CreateAsync(new CreateVideoInput("Titulo2", "video2.mp4", new MemoryStream()));

        ids.Should().HaveCount(2);
        ids[0].Should().NotBe(ids[1]);
    }

    // ── Ordem: storage -> repositorio -> fila ─────────────────────────────────

    [Fact]
    public async Task CreateAsync_OperationOrder_IsStorageThenRepositoryThenQueue()
    {
        var order = new List<string>();

        _fileStorage
            .Setup(x => x.SaveVideoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .Callback(() => order.Add("storage"))
            .ReturnsAsync("url");

        _repository
            .Setup(x => x.AddAsync(It.IsAny<VideoModel>()))
            .Callback<VideoModel>(_ => order.Add("repository"))
            .ReturnsAsync((VideoModel m) => m);

        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<VideoUploadedMessage>()))
            .Callback(() => order.Add("queue"))
            .Returns(Task.CompletedTask);

        await _sut.CreateAsync(new CreateVideoInput("Titulo", "video.mp4", new MemoryStream()));

        order.Should().ContainInOrder("storage", "repository", "queue");
    }

    // ── Tolerancia a falha na fila ────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_QueueFailure_VideoStillPersisted()
    {
        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("RabbitMQ indisponivel"));

        var result = await _sut.CreateAsync(new CreateVideoInput("Titulo", "video.mp4", new MemoryStream()));

        result.IsSuccess.Should().BeTrue();
        _repository.Verify(x => x.AddAsync(It.IsAny<VideoModel>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_QueueFailure_ReturnsVideoPendente()
    {
        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("RabbitMQ indisponivel"));

        var result = await _sut.CreateAsync(new CreateVideoInput("Titulo", "video.mp4", new MemoryStream()));

        result.Value.Status.Should().Be(StatusEnum.Pendente);
    }

    // ── Nao envia mensagem se validacao falhar ────────────────────────────────

    [Fact]
    public async Task CreateAsync_InvalidTitle_DoesNotPublishToQueue()
    {
        await _sut.CreateAsync(new CreateVideoInput(null, "video.mp4", new MemoryStream()));

        _messageSender.Verify(
            x => x.SendMessage(It.IsAny<string>(), It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidFilename_DoesNotPublishToQueue()
    {
        await _sut.CreateAsync(new CreateVideoInput("Titulo", null, new MemoryStream()));

        _messageSender.Verify(
            x => x.SendMessage(It.IsAny<string>(), It.IsAny<object>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidTitle_DoesNotCallFileStorage()
    {
        await _sut.CreateAsync(new CreateVideoInput(null, "video.mp4", new MemoryStream()));

        _fileStorage.Verify(
            x => x.SaveVideoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>()),
            Times.Never);
    }

    [Fact]
    public async Task CreateAsync_InvalidTitle_DoesNotPersistToRepository()
    {
        await _sut.CreateAsync(new CreateVideoInput(null, "video.mp4", new MemoryStream()));

        _repository.Verify(x => x.AddAsync(It.IsAny<VideoModel>()), Times.Never);
    }

    // ── Extensao do arquivo passada ao storage ────────────────────────────────

    [Theory]
    [InlineData("video.mp4",  ".mp4")]
    [InlineData("clip.avi",   ".avi")]
    [InlineData("movie.mkv",  ".mkv")]
    [InlineData("stream.webm",".webm")]
    public async Task CreateAsync_ExtractesCorrectExtensionToStorage(string filename, string expectedExtension)
    {
        string? savedExtension = null;
        _fileStorage
            .Setup(x => x.SaveVideoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .Callback<Guid, Guid, string, Stream>((_, _, ext, _) => savedExtension = ext)
            .ReturnsAsync("url");

        await _sut.CreateAsync(new CreateVideoInput("Titulo", filename, new MemoryStream()));

        savedExtension.Should().Be(expectedExtension);
    }

    [Fact]
    public async Task CreateAsync_PassesCorrectUserIdToFileStorage()
    {
        Guid? savedUserId = null;
        _fileStorage
            .Setup(x => x.SaveVideoAsync(It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .Callback<Guid, Guid, string, Stream>((uid, _, _, _) => savedUserId = uid)
            .ReturnsAsync("url");

        await _sut.CreateAsync(new CreateVideoInput("Titulo", "video.mp4", new MemoryStream()));

        savedUserId.Should().Be(_userId);
    }
}
