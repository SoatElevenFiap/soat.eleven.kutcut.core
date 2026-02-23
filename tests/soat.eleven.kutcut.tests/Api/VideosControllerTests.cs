using FluentAssertions;
using FluentResults;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using soat.eleven.kutcut.application.Dtos.Common;
using soat.eleven.kutcut.application.Dtos.Video;
using soat.eleven.kutcut.application.Interfaces;
using soat.eleven.kutcut.core.api.Controllers.V1;
using soat.eleven.kutcut.core.api.Controllers.V1.Dtos.Common;
using soat.eleven.kutcut.core.api.Controllers.V1.Dtos.Video;
using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.tests.Api;

public class VideosControllerTests
{
    private readonly Mock<IVideoService> _videoService;
    private readonly VideosController _sut;

    public VideosControllerTests()
    {
        _videoService = new Mock<IVideoService>();
        _sut = new VideosController(_videoService.Object);
    }

    private static VideoResult BuildVideoResult(Guid? id = null) => new(
        id ?? Guid.NewGuid(),
        "Test Video",
        Guid.NewGuid(),
        "video.mp4",
        StatusEnum.Pendente,
        "Pendente",
        DateTime.UtcNow,
        null);

    private static PagedResult<VideoResult> BuildPagedResult(
        IEnumerable<VideoResult>? items = null, int total = 1)
    {
        var list = items?.ToList() ?? new List<VideoResult> { BuildVideoResult() };
        return new PagedResult<VideoResult>(list, total, 1, 10, 1);
    }

    private static Mock<IFormFile> BuildFileMock(string filename = "video.mp4")
    {
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.OpenReadStream()).Returns(new MemoryStream());
        fileMock.Setup(f => f.FileName).Returns(filename);
        return fileMock;
    }

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task Create_ServiceReturnsSuccess_Returns201Created()
    {
        var videoResult = BuildVideoResult();
        _videoService.Setup(x => x.CreateAsync(It.IsAny<CreateVideoInput>()))
                     .ReturnsAsync(Result.Ok(videoResult));

        var result = await _sut.Create(new CreateVideoRequest("Test"), BuildFileMock().Object);

        result.Should().BeOfType<CreatedAtActionResult>()
              .Which.StatusCode.Should().Be(201);
    }

    [Fact]
    public async Task Create_ServiceReturnsSuccess_LocationPointsToGetById()
    {
        var videoResult = BuildVideoResult();
        _videoService.Setup(x => x.CreateAsync(It.IsAny<CreateVideoInput>()))
                     .ReturnsAsync(Result.Ok(videoResult));

        var result = await _sut.Create(new CreateVideoRequest("Test"), BuildFileMock().Object) as CreatedAtActionResult;

        result!.ActionName.Should().Be(nameof(VideosController.GetById));
    }

    [Fact]
    public async Task Create_ServiceReturnsFail_Returns400()
    {
        _videoService.Setup(x => x.CreateAsync(It.IsAny<CreateVideoInput>()))
                     .ReturnsAsync(Result.Fail<VideoResult>("Título obrigatório"));

        var result = await _sut.Create(new CreateVideoRequest(null), BuildFileMock().Object);

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    [Fact]
    public async Task Create_ServiceReturnsFail_ErrorResponseContainsMessage()
    {
        _videoService.Setup(x => x.CreateAsync(It.IsAny<CreateVideoInput>()))
                     .ReturnsAsync(Result.Fail<VideoResult>("Título obrigatório"));

        var result = await _sut.Create(new CreateVideoRequest(null), BuildFileMock().Object)
            as BadRequestObjectResult;

        result!.Value.Should().BeOfType<ErrorResponse>();
        var error = (ErrorResponse)result.Value!;
        error.Errors.Should().Contain("Título obrigatório");
    }

    // ── GetAll ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAll_ServiceReturnsSuccess_Returns200()
    {
        _videoService.Setup(x => x.GetAllAsync(1, 10))
                     .ReturnsAsync(Result.Ok(BuildPagedResult()));

        var result = await _sut.GetAll(new PaginationQuery { PageNumber = 1, PageSize = 10 });

        result.Should().BeOfType<OkObjectResult>()
              .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetAll_ReturnsPagedResponse()
    {
        _videoService.Setup(x => x.GetAllAsync(1, 10))
                     .ReturnsAsync(Result.Ok(BuildPagedResult()));

        var result = await _sut.GetAll(new PaginationQuery { PageNumber = 1, PageSize = 10 })
            as OkObjectResult;

        result!.Value.Should().BeOfType<PagedResponse<VideoResponse>>();
    }

    // ── GetById ───────────────────────────────────────────────────────────────

    [Fact]
    public async Task GetById_VideoFound_Returns200()
    {
        var videoResult = BuildVideoResult();
        _videoService.Setup(x => x.GetByIdAsync(videoResult.Id))
                     .ReturnsAsync(Result.Ok(videoResult));

        var result = await _sut.GetById(videoResult.Id);

        result.Should().BeOfType<OkObjectResult>()
              .Which.StatusCode.Should().Be(200);
    }

    [Fact]
    public async Task GetById_VideoFound_ReturnsVideoResponse()
    {
        var videoResult = BuildVideoResult();
        _videoService.Setup(x => x.GetByIdAsync(videoResult.Id))
                     .ReturnsAsync(Result.Ok(videoResult));

        var result = await _sut.GetById(videoResult.Id) as OkObjectResult;

        result!.Value.Should().BeOfType<VideoResponse>();
        var response = (VideoResponse)result.Value!;
        response.Id.Should().Be(videoResult.Id);
    }

    [Fact]
    public async Task GetById_VideoNotFound_Returns404()
    {
        _videoService.Setup(x => x.GetByIdAsync(It.IsAny<Guid>()))
                     .ReturnsAsync(Result.Fail<VideoResult>("Vídeo não encontrado."));

        var result = await _sut.GetById(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }

    // ── GetByStatus ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByStatus_ValidDefinedStatus_Returns200()
    {
        _videoService.Setup(x => x.GetByStatusAsync(StatusEnum.Pendente, 1, 10))
                     .ReturnsAsync(Result.Ok(BuildPagedResult()));

        var result = await _sut.GetByStatus(
            StatusEnum.Pendente,
            new PaginationQuery { PageNumber = 1, PageSize = 10 });

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task GetByStatus_InvalidStatus_Returns400()
    {
        var invalidStatus = (StatusEnum)999;

        var result = await _sut.GetByStatus(
            invalidStatus,
            new PaginationQuery { PageNumber = 1, PageSize = 10 });

        result.Should().BeOfType<BadRequestObjectResult>();
    }

    // ── UpdateTitle ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateTitle_ServiceReturnsSuccess_Returns200()
    {
        var videoResult = BuildVideoResult();
        _videoService.Setup(x => x.UpdateTitleAsync(videoResult.Id, "Novo Título"))
                     .ReturnsAsync(Result.Ok(videoResult));

        var result = await _sut.UpdateTitle(
            videoResult.Id,
            new UpdateVideoTitleRequest("Novo Título"));

        result.Should().BeOfType<OkObjectResult>();
    }

    [Fact]
    public async Task UpdateTitle_VideoNotFound_Returns404()
    {
        _videoService.Setup(x => x.UpdateTitleAsync(It.IsAny<Guid>(), It.IsAny<string?>()))
                     .ReturnsAsync(Result.Fail<VideoResult>("Vídeo não encontrado."));

        var result = await _sut.UpdateTitle(
            Guid.NewGuid(),
            new UpdateVideoTitleRequest("X"));

        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(404);
    }

    [Fact]
    public async Task UpdateTitle_ValidationError_Returns400()
    {
        _videoService.Setup(x => x.UpdateTitleAsync(It.IsAny<Guid>(), It.IsAny<string?>()))
                     .ReturnsAsync(Result.Fail<VideoResult>("O título do vídeo é obrigatório."));

        var result = await _sut.UpdateTitle(
            Guid.NewGuid(),
            new UpdateVideoTitleRequest(null));

        result.Should().BeOfType<ObjectResult>()
              .Which.StatusCode.Should().Be(400);
    }

    // ── Download ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task Download_FileFound_ReturnsFileResult()
    {
        var videoId = Guid.NewGuid();
        Stream stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _videoService.Setup(x => x.DownloadThumbnailsAsync(videoId))
                     .ReturnsAsync(Result.Ok(stream));

        var result = await _sut.Download(videoId);

        result.Should().BeOfType<FileStreamResult>();
    }

    [Fact]
    public async Task Download_FileFound_ContentTypeIsApplicationZip()
    {
        var videoId = Guid.NewGuid();
        Stream stream = new MemoryStream(new byte[] { 1, 2, 3 });

        _videoService.Setup(x => x.DownloadThumbnailsAsync(videoId))
                     .ReturnsAsync(Result.Ok(stream));

        var result = await _sut.Download(videoId) as FileStreamResult;

        result!.ContentType.Should().Be("application/zip");
    }

    [Fact]
    public async Task Download_FileNotFound_Returns404()
    {
        _videoService.Setup(x => x.DownloadThumbnailsAsync(It.IsAny<Guid>()))
                     .ReturnsAsync(Result.Fail<Stream>("Arquivo não encontrado."));

        var result = await _sut.Download(Guid.NewGuid());

        result.Should().BeOfType<NotFoundObjectResult>();
    }
}
