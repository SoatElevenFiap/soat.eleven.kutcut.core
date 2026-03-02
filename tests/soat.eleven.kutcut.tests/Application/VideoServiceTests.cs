using FluentAssertions;
using FluentResults;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using soat.eleven.kutcut.application.Dtos.Video;
using soat.eleven.kutcut.application.Exceptions;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.application.Services;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.queues.Interfaces;
using soat.eleven.kutcut.infra.Repository;
using soat.eleven.kutcut.infra.Storage;
using InfraStatus = soat.eleven.kutcut.infra.Models.Enums.StatusEnum;

namespace soat.eleven.kutcut.tests.Application;

public class VideoServiceTests
{
    private readonly Mock<IRepository<VideoModel>> _repository;
    private readonly Mock<IFileStorageService> _fileStorage;
    private readonly Mock<IUserContext> _userContext;
    private readonly Mock<IMessageSender> _messageSender;
    private readonly Mock<ILogger<VideoService>> _logger;
    private readonly VideoService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public VideoServiceTests()
    {
        _repository = new Mock<IRepository<VideoModel>>();
        _fileStorage = new Mock<IFileStorageService>();
        _userContext = new Mock<IUserContext>();
        _messageSender = new Mock<IMessageSender>();
        _logger = new Mock<ILogger<VideoService>>();

        var settings = Options.Create(new RabbitMQSettings
        {
            VideoUploadedQueueName = "video_uploaded"
        });

        _sut = new VideoService(
            _repository.Object,
            _fileStorage.Object,
            _userContext.Object,
            _messageSender.Object,
            settings,
            _logger.Object);

        SetupAuthenticatedUser();
    }

    private void SetupAuthenticatedUser()
    {
        _userContext.Setup(x => x.IsAuthenticated).Returns(true);
        _userContext.Setup(x => x.UserId).Returns(_userId);
    }

    private VideoModel BuildVideoModel(Guid? overrideUserId = null) => new()
    {
        Id = Guid.NewGuid(),
        Title = "Título Teste",
        UserId = overrideUserId ?? _userId,
        Filename = "video.mp4",
        Status = InfraStatus.Pendente,
        CreatedAt = DateTime.UtcNow
    };

    // ── CreateAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task CreateAsync_WithValidInput_ReturnsSuccess()
    {
        var input = new CreateVideoInput("Meu Vídeo", "video.mp4", new MemoryStream());

        _fileStorage.Setup(x => x.SaveVideoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync("https://blob.url/video.mp4");

        _repository.Setup(x => x.AddAsync(It.IsAny<VideoModel>()))
            .ReturnsAsync((VideoModel m) => m);

        _messageSender.Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);

        var result = await _sut.CreateAsync(input);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Meu Vídeo");
        result.Value.Status.Should().Be(StatusEnum.Pendente);
        result.Value.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task CreateAsync_WithInvalidTitle_ReturnsFail()
    {
        var input = new CreateVideoInput(null, "video.mp4", new MemoryStream());

        var result = await _sut.CreateAsync(input);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("título"));
    }

    [Fact]
    public async Task CreateAsync_WithNullFilename_ReturnsFail()
    {
        var input = new CreateVideoInput("Título", null, new MemoryStream());

        var result = await _sut.CreateAsync(input);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WhenNotAuthenticated_ThrowsUnauthorized()
    {
        _userContext.Setup(x => x.IsAuthenticated).Returns(false);
        var input = new CreateVideoInput("Título", "video.mp4", new MemoryStream());

        Func<Task> act = () => _sut.CreateAsync(input);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    [Fact]
    public async Task CreateAsync_WhenMessageSenderFails_StillReturnsSuccess()
    {
        var input = new CreateVideoInput("Meu Vídeo", "video.mp4", new MemoryStream());

        _fileStorage.Setup(x => x.SaveVideoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync("url");

        _repository.Setup(x => x.AddAsync(It.IsAny<VideoModel>()))
            .ReturnsAsync((VideoModel m) => m);

        _messageSender.Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<object>()))
            .ThrowsAsync(new Exception("RabbitMQ down"));

        var result = await _sut.CreateAsync(input);

        // Publishing failure is swallowed — video should still be created
        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_CallsFileStorageOnce()
    {
        var input = new CreateVideoInput("Título", "video.mp4", new MemoryStream());

        _fileStorage.Setup(x => x.SaveVideoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>()))
            .ReturnsAsync("url");

        _repository.Setup(x => x.AddAsync(It.IsAny<VideoModel>())).ReturnsAsync((VideoModel m) => m);

        await _sut.CreateAsync(input);

        _fileStorage.Verify(x => x.SaveVideoAsync(
            It.IsAny<Guid>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<Stream>()), Times.Once);
    }

    // ── GetAllAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsPagedResult()
    {
        var models = new List<VideoModel> { BuildVideoModel(), BuildVideoModel() };
        _repository.Setup(x => x.GetPagedAsync(
            1, 10,
            It.IsAny<System.Linq.Expressions.Expression<Func<VideoModel, bool>>>()))
            .ReturnsAsync((models, 2));

        var result = await _sut.GetAllAsync(1, 10);

        result.IsSuccess.Should().BeTrue();
        result.Value.Items.Should().HaveCount(2);
        result.Value.TotalCount.Should().Be(2);
        result.Value.PageNumber.Should().Be(1);
        result.Value.PageSize.Should().Be(10);
    }

    [Fact]
    public async Task GetAllAsync_CalculatesTotalPagesCorrectly()
    {
        var models = Enumerable.Range(0, 5).Select(_ => BuildVideoModel()).ToList();
        _repository.Setup(x => x.GetPagedAsync(
            1, 3,
            It.IsAny<System.Linq.Expressions.Expression<Func<VideoModel, bool>>>()))
            .ReturnsAsync((models.Take(3).ToList(), 5));

        var result = await _sut.GetAllAsync(1, 3);

        result.Value.TotalPages.Should().Be(2);
    }

    [Fact]
    public async Task GetAllAsync_WhenNotAuthenticated_ThrowsUnauthorized()
    {
        _userContext.Setup(x => x.IsAuthenticated).Returns(false);

        Func<Task> act = () => _sut.GetAllAsync(1, 10);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── GetByIdAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingVideoOwnedByUser_ReturnsSuccess()
    {
        var model = BuildVideoModel();
        _repository.Setup(x => x.GetByIdAsync(model.Id)).ReturnsAsync(model);

        var result = await _sut.GetByIdAsync(model.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(model.Id);
        result.Value.Title.Should().Be(model.Title);
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingVideo_ReturnsFail()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((VideoModel?)null);

        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("não encontrado"));
    }

    [Fact]
    public async Task GetByIdAsync_VideoOwnedByOtherUser_ReturnsFail()
    {
        var model = BuildVideoModel(overrideUserId: Guid.NewGuid());
        _repository.Setup(x => x.GetByIdAsync(model.Id)).ReturnsAsync(model);

        var result = await _sut.GetByIdAsync(model.Id);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_WhenNotAuthenticated_ThrowsUnauthorized()
    {
        _userContext.Setup(x => x.IsAuthenticated).Returns(false);

        Func<Task> act = () => _sut.GetByIdAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── GetByStatusAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task GetByStatusAsync_ReturnsFilteredPagedResult()
    {
        var models = new List<VideoModel> { BuildVideoModel() };
        _repository.Setup(x => x.GetPagedAsync(
            1, 5,
            It.IsAny<System.Linq.Expressions.Expression<Func<VideoModel, bool>>>()))
            .ReturnsAsync((models, 1));

        var result = await _sut.GetByStatusAsync(StatusEnum.Pendente, 1, 5);

        result.IsSuccess.Should().BeTrue();
        result.Value.TotalCount.Should().Be(1);
    }

    [Fact]
    public async Task GetByStatusAsync_WhenNotAuthenticated_ThrowsUnauthorized()
    {
        _userContext.Setup(x => x.IsAuthenticated).Returns(false);

        Func<Task> act = () => _sut.GetByStatusAsync(StatusEnum.Pendente, 1, 10);

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── UpdateTitleAsync ─────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTitleAsync_WithValidTitle_ReturnsSuccess()
    {
        var model = BuildVideoModel();
        _repository.Setup(x => x.GetByIdAsync(model.Id)).ReturnsAsync(model);
        _repository.Setup(x => x.UpdateAsync(It.IsAny<VideoModel>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateTitleAsync(model.Id, "Novo Título");

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTitleAsync_VideoNotFound_ReturnsFail()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((VideoModel?)null);

        var result = await _sut.UpdateTitleAsync(Guid.NewGuid(), "Novo Título");

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("não encontrado"));
    }

    [Fact]
    public async Task UpdateTitleAsync_VideoOwnedByOtherUser_ThrowsForbidden()
    {
        var model = BuildVideoModel(overrideUserId: Guid.NewGuid());
        _repository.Setup(x => x.GetByIdAsync(model.Id)).ReturnsAsync(model);

        Func<Task> act = () => _sut.UpdateTitleAsync(model.Id, "Novo Título");

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task UpdateTitleAsync_WithNullTitle_ReturnsFail()
    {
        var model = BuildVideoModel();
        _repository.Setup(x => x.GetByIdAsync(model.Id)).ReturnsAsync(model);

        var result = await _sut.UpdateTitleAsync(model.Id, null);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateTitleAsync_WhenNotAuthenticated_ThrowsUnauthorized()
    {
        _userContext.Setup(x => x.IsAuthenticated).Returns(false);

        Func<Task> act = () => _sut.UpdateTitleAsync(Guid.NewGuid(), "Título");

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }

    // ── UpdateStatusAsync ────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_ValidStatus_ReturnsSuccess()
    {
        var model = BuildVideoModel();
        _repository.Setup(x => x.GetByIdAsync(model.Id)).ReturnsAsync(model);
        _repository.Setup(x => x.UpdateAsync(It.IsAny<VideoModel>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateStatusAsync(model.Id, StatusEnum.Uploaded);

        result.IsSuccess.Should().BeTrue();
    }

    [Theory]
    [InlineData(StatusEnum.Pendente)]
    [InlineData(StatusEnum.Uploaded)]
    [InlineData(StatusEnum.EmProcessamento)]
    [InlineData(StatusEnum.ProcessadoComSucesso)]
    [InlineData(StatusEnum.ProcessadoComErro)]
    public async Task UpdateStatusAsync_AllStatuses_AreAccepted(StatusEnum status)
    {
        var model = BuildVideoModel();
        _repository.Setup(x => x.GetByIdAsync(model.Id)).ReturnsAsync(model);
        _repository.Setup(x => x.UpdateAsync(It.IsAny<VideoModel>())).Returns(Task.CompletedTask);

        var result = await _sut.UpdateStatusAsync(model.Id, status);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public async Task UpdateStatusAsync_VideoNotFound_ReturnsFail()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((VideoModel?)null);

        var result = await _sut.UpdateStatusAsync(Guid.NewGuid(), StatusEnum.Uploaded);

        result.IsFailed.Should().BeTrue();
    }

    // ── DownloadThumbnailsAsync ──────────────────────────────────────────────

    [Fact]
    public async Task DownloadThumbnailsAsync_FileExists_ReturnsStream()
    {
        var model = BuildVideoModel();
        var stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _repository.Setup(x => x.GetByIdAsync(model.Id)).ReturnsAsync(model);
        _fileStorage.Setup(x => x.GetThumbnailZipAsync(model.UserId, model.Id)).ReturnsAsync(stream);

        var result = await _sut.DownloadThumbnailsAsync(model.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeSameAs(stream);
    }

    [Fact]
    public async Task DownloadThumbnailsAsync_VideoNotFound_ReturnsFail()
    {
        _repository.Setup(x => x.GetByIdAsync(It.IsAny<Guid>())).ReturnsAsync((VideoModel?)null);

        var result = await _sut.DownloadThumbnailsAsync(Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("não encontrado"));
    }

    [Fact]
    public async Task DownloadThumbnailsAsync_FileNotFound_ReturnsFail()
    {
        var model = BuildVideoModel();
        _repository.Setup(x => x.GetByIdAsync(model.Id)).ReturnsAsync(model);
        _fileStorage.Setup(x => x.GetThumbnailZipAsync(model.UserId, model.Id))
            .ReturnsAsync((Stream?)null);

        var result = await _sut.DownloadThumbnailsAsync(model.Id);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("thumbnails"));
    }

    [Fact]
    public async Task DownloadThumbnailsAsync_OtherUsersVideo_ThrowsForbidden()
    {
        var model = BuildVideoModel(overrideUserId: Guid.NewGuid());
        _repository.Setup(x => x.GetByIdAsync(model.Id)).ReturnsAsync(model);

        Func<Task> act = () => _sut.DownloadThumbnailsAsync(model.Id);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task DownloadThumbnailsAsync_WhenNotAuthenticated_ThrowsUnauthorized()
    {
        _userContext.Setup(x => x.IsAuthenticated).Returns(false);

        Func<Task> act = () => _sut.DownloadThumbnailsAsync(Guid.NewGuid());

        await act.Should().ThrowAsync<UnauthorizedAccessException>();
    }
}
