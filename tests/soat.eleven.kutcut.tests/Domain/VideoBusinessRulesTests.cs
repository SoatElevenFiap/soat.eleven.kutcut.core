using FluentAssertions;
using soat.eleven.kutcut.domain.Entities;
using soat.eleven.kutcut.domain.Enums;

namespace soat.eleven.kutcut.tests.Domain;

/// <summary>
/// Testes aprofundados das regras de negocio das entidades de dominio.
/// Cobre restricoes, mensagens de erro exatas, transicoes de estado e invariantes.
/// </summary>
public class VideoBusinessRulesTests
{
    private readonly Guid _validUserId = Guid.NewGuid();
    private const string ValidTitle    = "Video de Treinamento";
    private const string ValidFilename = "treinamento.mp4";

    // ── Titulo: regras de negocio ─────────────────────────────────────────────

    [Fact]
    public void Create_NullTitle_ErrorMessageMentionsTitulo()
    {
        var result = Video.Create(null, _validUserId, ValidFilename);

        result.Errors.Should().ContainSingle(e => e.Message.Contains("título"));
    }

    [Fact]
    public void Create_WhitespaceTitle_ErrorMessageMentionsTitulo()
    {
        var result = Video.Create("   ", _validUserId, ValidFilename);

        result.Errors.Should().ContainSingle(e => e.Message.Contains("título"));
    }

    [Fact]
    public void Create_EmptyStringTitle_ReturnsFail()
    {
        var result = Video.Create("", _validUserId, ValidFilename);

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void Create_SingleCharTitle_IsAccepted()
    {
        var result = Video.Create("A", _validUserId, ValidFilename);

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_TitleWithSpecialCharacters_IsAccepted()
    {
        var result = Video.Create("Video #1 - Aula (Final)", _validUserId, ValidFilename);

        result.IsSuccess.Should().BeTrue();
        result.Value.Title.Should().Be("Video #1 - Aula (Final)");
    }

    [Fact]
    public void Create_TitleWithLeadingAndTrailingSpaces_IsAccepted()
    {
        // soh whitespace falha — titulo com espacos ao redor mas conteudo valido eh aceito
        var result = Video.Create("  Titulo Valido  ", _validUserId, ValidFilename);

        result.IsSuccess.Should().BeTrue();
    }

    // ── UserId: regras de negocio ─────────────────────────────────────────────

    [Fact]
    public void Create_EmptyUserId_ErrorMessageMentionsUsuario()
    {
        var result = Video.Create(ValidTitle, Guid.Empty, ValidFilename);

        result.Errors.Should().ContainSingle(e => e.Message.Contains("usuário"));
    }

    [Fact]
    public void Create_ValidUserId_IsAccepted()
    {
        var result = Video.Create(ValidTitle, Guid.NewGuid(), ValidFilename);

        result.IsSuccess.Should().BeTrue();
    }

    // ── Filename: regras de negocio ───────────────────────────────────────────

    [Fact]
    public void Create_NullFilename_ErrorMessageMentionsArquivo()
    {
        var result = Video.Create(ValidTitle, _validUserId, null);

        result.Errors.Should().ContainSingle(e => e.Message.Contains("arquivo"));
    }

    [Fact]
    public void Create_WhitespaceFilename_ReturnsFail()
    {
        var result = Video.Create(ValidTitle, _validUserId, "   ");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void Create_FilenameWithExtension_IsPreservedExactly()
    {
        var result = Video.Create(ValidTitle, _validUserId, "meu_video.mp4");

        result.Value.Filename.Should().Be("meu_video.mp4");
    }

    // ── Multiplos erros simultaneos ───────────────────────────────────────────

    [Fact]
    public void Create_AllFieldsInvalid_ReturnsThreeErrors()
    {
        var result = Video.Create(null, Guid.Empty, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(3);
    }

    [Fact]
    public void Create_TitleAndFilenameNull_ReturnsTwoErrors()
    {
        var result = Video.Create(null, _validUserId, null);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    [Fact]
    public void Create_TitleNullAndEmptyUserId_ReturnsTwoErrors()
    {
        var result = Video.Create(null, Guid.Empty, ValidFilename);

        result.IsFailed.Should().BeTrue();
        result.Errors.Should().HaveCount(2);
    }

    // ── Estado inicial apos criacao ───────────────────────────────────────────

    [Fact]
    public void Create_NewVideo_HasStatusPendente()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.Status.Should().Be(StatusEnum.Pendente);
    }

    [Fact]
    public void Create_NewVideo_HasNullUpdatedAt()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void Create_NewVideo_HasCreatedAtNearNow()
    {
        var before = DateTime.UtcNow;
        var video  = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;
        var after  = DateTime.UtcNow;

        video.CreatedAt.Should().BeOnOrAfter(before.AddSeconds(-1));
        video.CreatedAt.Should().BeOnOrBefore(after.AddSeconds(1));
    }

    [Fact]
    public void Create_NewVideo_IdIsNotEmpty()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.Id.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void Create_TwoVideos_HaveDifferentIds()
    {
        var v1 = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;
        var v2 = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        v1.Id.Should().NotBe(v2.Id);
    }

    // ── UpdateTitle: regras de negocio ────────────────────────────────────────

    [Fact]
    public void UpdateTitle_ValidTitle_TitleIsUpdated()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.UpdateTitle("Novo Titulo");

        video.Title.Should().Be("Novo Titulo");
    }

    [Fact]
    public void UpdateTitle_ValidTitle_UpdatedAtIsSet()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.UpdateTitle("Novo Titulo");

        video.UpdatedAt.Should().NotBeNull();
        video.UpdatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void UpdateTitle_NullTitle_ErrorMessageMentionsTitulo()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        var result = video.UpdateTitle(null);

        result.Errors.Should().ContainSingle(e => e.Message.Contains("título"));
    }

    [Fact]
    public void UpdateTitle_EmptyTitle_ReturnsFail()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        var result = video.UpdateTitle("");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void UpdateTitle_WhitespaceTitle_ReturnsFail()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        var result = video.UpdateTitle("   ");

        result.IsFailed.Should().BeTrue();
    }

    [Fact]
    public void UpdateTitle_FailedUpdate_OriginalTitlePreserved()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.UpdateTitle(null);

        video.Title.Should().Be(ValidTitle);
    }

    [Fact]
    public void UpdateTitle_FailedUpdate_UpdatedAtRemainsNull()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.UpdateTitle(null);

        video.UpdatedAt.Should().BeNull();
    }

    [Fact]
    public void UpdateTitle_CalledMultipleTimes_LastTitleWins()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.UpdateTitle("Segundo Titulo");
        video.UpdateTitle("Terceiro Titulo");

        video.Title.Should().Be("Terceiro Titulo");
    }

    [Fact]
    public void UpdateTitle_CalledMultipleTimes_UpdatedAtIsRefreshed()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.UpdateTitle("Segundo");
        var firstUpdate = video.UpdatedAt;

        // Pequena espera para garantir diferenca temporal
        System.Threading.Thread.Sleep(10);
        video.UpdateTitle("Terceiro");

        video.UpdatedAt.Should().BeOnOrAfter(firstUpdate!.Value);
    }

    // ── SetStatus: transicoes de estado ───────────────────────────────────────

    [Theory]
    [InlineData(StatusEnum.Pendente)]
    [InlineData(StatusEnum.Uploaded)]
    [InlineData(StatusEnum.EmProcessamento)]
    [InlineData(StatusEnum.ProcessadoComSucesso)]
    [InlineData(StatusEnum.ProcessadoComErro)]
    public void SetStatus_AnyStatus_StatusIsUpdated(StatusEnum status)
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
    public void SetStatus_Pendente_ToUploaded_IsAllowed()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;
        video.Status.Should().Be(StatusEnum.Pendente);

        video.SetStatus(StatusEnum.Uploaded);

        video.Status.Should().Be(StatusEnum.Uploaded);
    }

    [Fact]
    public void SetStatus_Uploaded_ToEmProcessamento_IsAllowed()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.SetStatus(StatusEnum.Uploaded);
        video.SetStatus(StatusEnum.EmProcessamento);

        video.Status.Should().Be(StatusEnum.EmProcessamento);
    }

    [Fact]
    public void SetStatus_EmProcessamento_ToProcessadoComSucesso_IsAllowed()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.SetStatus(StatusEnum.EmProcessamento);
        video.SetStatus(StatusEnum.ProcessadoComSucesso);

        video.Status.Should().Be(StatusEnum.ProcessadoComSucesso);
    }

    [Fact]
    public void SetStatus_EmProcessamento_ToProcessadoComErro_IsAllowed()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.SetStatus(StatusEnum.EmProcessamento);
        video.SetStatus(StatusEnum.ProcessadoComErro);

        video.Status.Should().Be(StatusEnum.ProcessadoComErro);
    }

    [Fact]
    public void SetStatus_CalledInSequence_AlwaysReflectsLastStatus()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;
        var statuses = new[]
        {
            StatusEnum.Uploaded,
            StatusEnum.EmProcessamento,
            StatusEnum.ProcessadoComSucesso
        };

        foreach (var s in statuses)
            video.SetStatus(s);

        video.Status.Should().Be(StatusEnum.ProcessadoComSucesso);
    }

    // ── Invariantes: Id e UserId nao mudam ────────────────────────────────────

    [Fact]
    public void UpdateTitle_DoesNotChangeId()
    {
        var video  = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;
        var origId = video.Id;

        video.UpdateTitle("Novo Titulo");

        video.Id.Should().Be(origId);
    }

    [Fact]
    public void UpdateTitle_DoesNotChangeUserId()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.UpdateTitle("Novo Titulo");

        video.UserId.Should().Be(_validUserId);
    }

    [Fact]
    public void SetStatus_DoesNotChangeId()
    {
        var video  = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;
        var origId = video.Id;

        video.SetStatus(StatusEnum.Uploaded);

        video.Id.Should().Be(origId);
    }

    [Fact]
    public void SetStatus_DoesNotChangeUserId()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.SetStatus(StatusEnum.Uploaded);

        video.UserId.Should().Be(_validUserId);
    }

    [Fact]
    public void SetStatus_DoesNotChangeFilename()
    {
        var video = Video.Create(ValidTitle, _validUserId, ValidFilename).Value;

        video.SetStatus(StatusEnum.Uploaded);

        video.Filename.Should().Be(ValidFilename);
    }

    // ── StatusEnum: valores numericos corretos ────────────────────────────────

    [Theory]
    [InlineData(StatusEnum.Pendente,             1)]
    [InlineData(StatusEnum.Uploaded,             2)]
    [InlineData(StatusEnum.EmProcessamento,      3)]
    [InlineData(StatusEnum.ProcessadoComSucesso, 4)]
    [InlineData(StatusEnum.ProcessadoComErro,    5)]
    public void StatusEnum_NumericValues_AreCorrect(StatusEnum status, int expected)
    {
        ((int)status).Should().Be(expected);
    }

    // ── NotificationType: valores numericos corretos ──────────────────────────

    [Theory]
    [InlineData(NotificationTypeEnum.ProcessadoComSucesso, 1)]
    [InlineData(NotificationTypeEnum.ProcessadoComErro,    2)]
    public void NotificationTypeEnum_NumericValues_AreCorrect(NotificationTypeEnum type, int expected)
    {
        ((int)type).Should().Be(expected);
    }
}
