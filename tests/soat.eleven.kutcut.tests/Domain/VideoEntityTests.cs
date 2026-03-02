using FluentAssertions;
using soat.eleven.kutcut.domain.Entities;
using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.tests.Domain;

public class VideoEntityTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private const string ValidTitle = "Meu Vídeo";
    private const string ValidFilename = "video.mp4";

    // ── Create ───────────────────────────────────────────────────────────────

    [Fact]
    public void Create_WithValidData_ReturnsSuccessResult()
    {
        var result = Video.Create(ValidTitle, _validUserId, ValidFilename);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
    }

    [Fact]
    public void Create_WithValidData_SetsAllPropertiesCorrectly()
    {
        var result = Video.Create(ValidTitle, _validUserId, ValidFilename);

        var video = result.Value;
        video.Id.Should().NotBe(Guid.Empty);
        video.Title.Should().Be(ValidTitle);
        video.UserId.Should().Be(_validUserId);
        video.Filename.Should().Be(ValidFilename);
        video.Status.Should().Be(StatusEnum.Pendente);
        video.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
        video.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_WithNullTitle_ReturnsFailedResult()
    {
        var result = Video.Create(null, _validUserId, ValidFilename);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("título"));
    }

    [Fact]
    public void Create_WithWhitespaceTitle_ReturnsFailedResult()
    {
        var result = Video.Create("   ", _validUserId, ValidFilename);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("título"));
    }

    [Fact]
    public void Create_WithEmptyTitle_ReturnsFailedResult()
    {
        var result = Video.Create(string.Empty, _validUserId, ValidFilename);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void Create_WithEmptyUserId_ReturnsFailedResult()
    {
        var result = Video.Create(ValidTitle, Guid.Empty, ValidFilename);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("usuário"));
    }

    [Fact]
    public void Create_WithNullFilename_ReturnsFailedResult()
    {
        var result = Video.Create(ValidTitle, _validUserId, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("arquivo"));
    }

    [Fact]
    public void Create_WithWhitespaceFilename_ReturnsFailedResult()
    {
        var result = Video.Create(ValidTitle, _validUserId, "   ");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void Create_WithMultipleInvalidFields_ReturnsAllErrors()
    {
        var result = Video.Create(null, Guid.Empty, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Create_EachCallGeneratesUniqueId()
    {
        var r1 = Video.Create(ValidTitle, _validUserId, ValidFilename);
        var r2 = Video.Create(ValidTitle, _validUserId, ValidFilename);

        r1.Value.Id.Should().NotBe(r2.Value.Id);
    }

    [Fact]
    public void Create_InitialStatus_IsPendente()
    {
        var result = Video.Create(ValidTitle, _validUserId, ValidFilename);

        result.Value.Status.Should().Be(StatusEnum.Pendente);
    }

    // ── UpdateTitle ──────────────────────────────────────────────────────────

    [Fact]
    public void UpdateTitle_WithValidTitle_ReturnsSuccess()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        var result = video.UpdateTitle("Novo Título");

        result.IsSuccess.Should().BeTrue();
        video.Title.Should().Be("Novo Título");
    }

    [Fact]
    public void UpdateTitle_WithValidTitle_SetsUpdatedAt()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.UpdateTitle("Novo Título");

        video.UpdatedAt.Should().NotBeNull();
        video.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateTitle_WithNullTitle_ReturnsFail()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        var result = video.UpdateTitle(null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().ContainSingle(e => e.Message.Contains("título"));
    }

    [Fact]
    public void UpdateTitle_WithEmptyTitle_ReturnsFail()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        var result = video.UpdateTitle(string.Empty);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void UpdateTitle_WithWhitespaceTitle_ReturnsFail()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        var result = video.UpdateTitle("   ");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void UpdateTitle_OnFail_DoesNotChangeTitleOrTimestamp()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;
        var originalUpdatedAt = video.UpdatedAt;

        video.UpdateTitle(null);

        video.Title.Should().Be(ValidTitle);
        video.UpdatedAt.Should().Be(originalUpdatedAt);
    }

    // ── SetStatus ────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(StatusEnum.Pendente)]
    [InlineData(StatusEnum.Uploaded)]
    [InlineData(StatusEnum.EmProcessamento)]
    [InlineData(StatusEnum.ProcessadoComSucesso)]
    [InlineData(StatusEnum.ProcessadoComErro)]
    public void SetStatus_AllStatusValues_AreApplied(StatusEnum status)
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.SetStatus(status);

        video.Status.Should().Be(status);
    }

    [Fact]
    public void SetStatus_SetsUpdatedAt()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.SetStatus(StatusEnum.Uploaded);

        video.UpdatedAt.Should().NotBeNull();
        video.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void SetStatus_OverridesPreviousStatus()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.SetStatus(StatusEnum.EmProcessamento);
        video.SetStatus(StatusEnum.ProcessadoComSucesso);

        video.Status.Should().Be(StatusEnum.ProcessadoComSucesso);
    }
}
