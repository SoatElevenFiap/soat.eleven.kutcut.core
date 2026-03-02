using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using soat.eleven.kutcut.application.Dtos.Video;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.application.Services;
using soat.eleven.kutcut.domain.Enums;
using soat.eleven.kutcut.infra.Configuration;
using soat.eleven.kutcut.infra.Context;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.queues.Interfaces;
using soat.eleven.kutcut.infra.Repository;
using soat.eleven.kutcut.infra.Storage;
using InfraStatus = soat.eleven.kutcut.infra.Models.Enums.StatusEnum;

namespace soat.eleven.kutcut.tests.Application;

/// <summary>
/// Testes de VideoService usando o repositorio real com banco InMemory.
/// Valida que as operacoes CRUD do repositorio sao chamadas corretamente
/// e que os dados sao persistidos e recuperados com integridade.
/// </summary>
public class VideoServiceRepositoryTests : IDisposable
{
    private readonly AppDbContext _dbContext;
    private readonly Repository<VideoModel> _repository;
    private readonly Mock<IFileStorageService> _fileStorage;
    private readonly Mock<IUserContext> _userContext;
    private readonly Mock<IMessageSender> _messageSender;
    private readonly Mock<ILogger<VideoService>> _logger;
    private readonly VideoService _sut;
    private readonly Guid _userId = Guid.NewGuid();

    public VideoServiceRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _dbContext   = new AppDbContext(options);
        _repository  = new Repository<VideoModel>(_dbContext);
        _fileStorage = new Mock<IFileStorageService>();
        _userContext = new Mock<IUserContext>();
        _messageSender = new Mock<IMessageSender>();
        _logger = new Mock<ILogger<VideoService>>();

        var settings = Options.Create(new RabbitMQSettings
        {
            VideoUploadedQueueName = "video_uploaded"
        });

        _sut = new VideoService(
            _repository,
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

        _messageSender
            .Setup(x => x.SendMessage(It.IsAny<string>(), It.IsAny<object>()))
            .Returns(Task.CompletedTask);
    }

    public void Dispose() => _dbContext.Dispose();

    // ── CreateAsync: persistencia no banco ───────────────────────────────────

    [Fact]
    public async Task CreateAsync_PersistsVideoToDatabase()
    {
        var result = await _sut.CreateAsync(new CreateVideoInput("Meu Video", "video.mp4", new MemoryStream()));

        result.IsSuccess.Should().BeTrue();
        var stored = await _dbContext.Videos.FindAsync(result.Value.Id);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_PersistsCorrectTitle()
    {
        var result = await _sut.CreateAsync(new CreateVideoInput("Titulo Especifico", "video.mp4", new MemoryStream()));

        var stored = await _dbContext.Videos.FindAsync(result.Value.Id);
        stored!.Title.Should().Be("Titulo Especifico");
    }

    [Fact]
    public async Task CreateAsync_PersistsCorrectUserId()
    {
        var result = await _sut.CreateAsync(new CreateVideoInput("Video", "video.mp4", new MemoryStream()));

        var stored = await _dbContext.Videos.FindAsync(result.Value.Id);
        stored!.UserId.Should().Be(_userId);
    }

    [Fact]
    public async Task CreateAsync_PersistsCorrectFilename()
    {
        var result = await _sut.CreateAsync(new CreateVideoInput("Video", "meu_arquivo.mp4", new MemoryStream()));

        var stored = await _dbContext.Videos.FindAsync(result.Value.Id);
        stored!.Filename.Should().Be("meu_arquivo.mp4");
    }

    [Fact]
    public async Task CreateAsync_PersistsStatusPendente()
    {
        var result = await _sut.CreateAsync(new CreateVideoInput("Video", "video.mp4", new MemoryStream()));

        var stored = await _dbContext.Videos.FindAsync(result.Value.Id);
        stored!.Status.Should().Be(InfraStatus.Pendente);
    }

    [Fact]
    public async Task CreateAsync_TwoVideos_BothPersistedWithDifferentIds()
    {
        var r1 = await _sut.CreateAsync(new CreateVideoInput("Video 1", "v1.mp4", new MemoryStream()));
        var r2 = await _sut.CreateAsync(new CreateVideoInput("Video 2", "v2.mp4", new MemoryStream()));

        r1.Value.Id.Should().NotBe(r2.Value.Id);
        _dbContext.Videos.Should().HaveCount(2);
    }

    // ── GetAllAsync: pagina videos do usuario ─────────────────────────────────

    [Fact]
    public async Task GetAllAsync_ReturnsOnlyCurrentUserVideos()
    {
        // Videos do usuario atual
        await _dbContext.Videos.AddRangeAsync(
            BuildStoredVideo("V1", _userId),
            BuildStoredVideo("V2", _userId));

        // Video de outro usuario (nao deveria aparecer)
        await _dbContext.Videos.AddAsync(BuildStoredVideo("V3", Guid.NewGuid()));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync(1, 10);

        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().OnlyContain(v => v.UserId == _userId);
    }

    [Fact]
    public async Task GetAllAsync_ReturnsEmptyWhenUserHasNoVideos()
    {
        // Somente video de outro usuario
        await _dbContext.Videos.AddAsync(BuildStoredVideo("V1", Guid.NewGuid()));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync(1, 10);

        result.Value.TotalCount.Should().Be(0);
        result.Value.Items.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_Pagination_ReturnsCorrectPage()
    {
        for (int i = 1; i <= 5; i++)
        {
            await _dbContext.Videos.AddAsync(BuildStoredVideo($"Video {i}", _userId));
        }
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetAllAsync(1, 3);

        result.Value.Items.Should().HaveCount(3);
        result.Value.TotalCount.Should().Be(5);
        result.Value.TotalPages.Should().Be(2);
    }

    // ── GetByIdAsync: busca por ID com verificacao de propriedade ─────────────

    [Fact]
    public async Task GetByIdAsync_ExistingVideo_ReturnsCorrectVideo()
    {
        var model = BuildStoredVideo("Meu Video", _userId);
        await _dbContext.Videos.AddAsync(model);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(model.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Id.Should().Be(model.Id);
        result.Value.Title.Should().Be("Meu Video");
    }

    [Fact]
    public async Task GetByIdAsync_VideoFromAnotherUser_ReturnsFail()
    {
        var model = BuildStoredVideo("Video Alheio", Guid.NewGuid());
        await _dbContext.Videos.AddAsync(model);
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByIdAsync(model.Id);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsFail()
    {
        var result = await _sut.GetByIdAsync(Guid.NewGuid());

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("não encontrado"));
    }

    // ── GetByStatusAsync: filtro por status ───────────────────────────────────

    [Fact]
    public async Task GetByStatusAsync_ReturnsOnlyVideosWithMatchingStatus()
    {
        await _dbContext.Videos.AddRangeAsync(
            BuildStoredVideo("Pendente 1",  _userId, InfraStatus.Pendente),
            BuildStoredVideo("Pendente 2",  _userId, InfraStatus.Pendente),
            BuildStoredVideo("Uploaded 1",  _userId, InfraStatus.Uploaded));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByStatusAsync(StatusEnum.Pendente, 1, 10);

        result.Value.TotalCount.Should().Be(2);
        result.Value.Items.Should().OnlyContain(v => v.Status == StatusEnum.Pendente);
    }

    [Fact]
    public async Task GetByStatusAsync_ExcludesOtherUsersVideos()
    {
        await _dbContext.Videos.AddRangeAsync(
            BuildStoredVideo("Meu Video",      _userId,          InfraStatus.Uploaded),
            BuildStoredVideo("Video Alheio",   Guid.NewGuid(),   InfraStatus.Uploaded));
        await _dbContext.SaveChangesAsync();

        var result = await _sut.GetByStatusAsync(StatusEnum.Uploaded, 1, 10);

        result.Value.TotalCount.Should().Be(1);
        result.Value.Items.Should().OnlyContain(v => v.UserId == _userId);
    }

    // ── UpdateTitleAsync: persistencia da atualizacao ─────────────────────────

    [Fact]
    public async Task UpdateTitleAsync_PersistsNewTitle()
    {
        var model = BuildStoredVideo("Titulo Original", _userId);
        await _dbContext.Videos.AddAsync(model);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var result = await _sut.UpdateTitleAsync(model.Id, "Titulo Atualizado");

        result.IsSuccess.Should().BeTrue();
        var stored = await _dbContext.Videos.FindAsync(model.Id);
        stored!.Title.Should().Be("Titulo Atualizado");
    }

    [Fact]
    public async Task UpdateTitleAsync_SetsUpdatedAt()
    {
        var model = BuildStoredVideo("Titulo Original", _userId);
        await _dbContext.Videos.AddAsync(model);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        await _sut.UpdateTitleAsync(model.Id, "Novo Titulo");

        var stored = await _dbContext.Videos.FindAsync(model.Id);
        stored!.UpdatedAt.Should().NotBeNull();
        stored.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public async Task UpdateTitleAsync_DoesNotChangeFilenameOrUserId()
    {
        var model = BuildStoredVideo("Titulo", _userId);
        await _dbContext.Videos.AddAsync(model);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        await _sut.UpdateTitleAsync(model.Id, "Novo Titulo");

        var stored = await _dbContext.Videos.FindAsync(model.Id);
        stored!.UserId.Should().Be(_userId);
        stored.Filename.Should().Be(model.Filename);
    }

    // ── UpdateStatusAsync: persistencia do status ─────────────────────────────

    [Fact]
    public async Task UpdateStatusAsync_PersistsNewStatus()
    {
        var model = BuildStoredVideo("Video", _userId, InfraStatus.Pendente);
        await _dbContext.Videos.AddAsync(model);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        var result = await _sut.UpdateStatusAsync(model.Id, StatusEnum.Uploaded);

        result.IsSuccess.Should().BeTrue();
        var stored = await _dbContext.Videos.FindAsync(model.Id);
        stored!.Status.Should().Be(InfraStatus.Uploaded);
    }

    [Theory]
    [InlineData(StatusEnum.Uploaded,             InfraStatus.Uploaded)]
    [InlineData(StatusEnum.EmProcessamento,      InfraStatus.EmProcessamento)]
    [InlineData(StatusEnum.ProcessadoComSucesso, InfraStatus.ProcessadoComSucesso)]
    [InlineData(StatusEnum.ProcessadoComErro,    InfraStatus.ProcessadoComErro)]
    public async Task UpdateStatusAsync_AllStatuses_PersistedCorrectly(
        StatusEnum domainStatus, InfraStatus expectedInfraStatus)
    {
        var model = BuildStoredVideo("Video", _userId);
        await _dbContext.Videos.AddAsync(model);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        await _sut.UpdateStatusAsync(model.Id, domainStatus);

        var stored = await _dbContext.Videos.FindAsync(model.Id);
        stored!.Status.Should().Be(expectedInfraStatus);
    }

    // ── DownloadThumbnailsAsync: integracao com storage ───────────────────────

    [Fact]
    public async Task DownloadThumbnailsAsync_CallsStorageWithCorrectVideoAndUserId()
    {
        var model = BuildStoredVideo("Video", _userId);
        await _dbContext.Videos.AddAsync(model);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        _fileStorage
            .Setup(x => x.GetThumbnailZipAsync(model.UserId, model.Id))
            .ReturnsAsync(new MemoryStream(new byte[] { 1, 2, 3 }));

        await _sut.DownloadThumbnailsAsync(model.Id);

        _fileStorage.Verify(
            x => x.GetThumbnailZipAsync(model.UserId, model.Id),
            Times.Once);
    }

    [Fact]
    public async Task DownloadThumbnailsAsync_WhenFileExists_ReturnsStreamContent()
    {
        var content = new byte[] { 10, 20, 30, 40 };
        var model   = BuildStoredVideo("Video", _userId);
        await _dbContext.Videos.AddAsync(model);
        await _dbContext.SaveChangesAsync();
        _dbContext.ChangeTracker.Clear();

        _fileStorage
            .Setup(x => x.GetThumbnailZipAsync(model.UserId, model.Id))
            .ReturnsAsync(new MemoryStream(content));

        var result = await _sut.DownloadThumbnailsAsync(model.Id);

        result.IsSuccess.Should().BeTrue();
        var bytes = new byte[content.Length];
        await result.Value.ReadAsync(bytes);
        bytes.Should().BeEquivalentTo(content);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static VideoModel BuildStoredVideo(
        string title,
        Guid userId,
        InfraStatus status = InfraStatus.Pendente)
        => new()
        {
            Id        = Guid.NewGuid(),
            Title     = title,
            UserId    = userId,
            Filename  = $"{title.Replace(" ", "_").ToLower()}.mp4",
            Status    = status,
            CreatedAt = DateTime.UtcNow
        };
}
