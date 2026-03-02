using FluentAssertions;
using soat.eleven.kutcut.infra.Storage;

namespace soat.eleven.kutcut.tests.Infra;

public class LocalFileStorageServiceTests : IDisposable
{
    private readonly string _basePath;
    private readonly LocalFileStorageService _sut;

    public LocalFileStorageServiceTests()
    {
        _basePath = Path.Combine(Path.GetTempPath(), $"kutcut_tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_basePath);
        _sut = new LocalFileStorageService(_basePath);
    }

    public void Dispose()
    {
        if (Directory.Exists(_basePath))
            Directory.Delete(_basePath, recursive: true);
    }

    // ── SaveVideoAsync ────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveVideoAsync_CreatesFileAtExpectedPath()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var content = new byte[] { 1, 2, 3, 4, 5 };

        using var stream = new MemoryStream(content);
        var path = await _sut.SaveVideoAsync(userId, videoId, ".mp4", stream);

        File.Exists(path).Should().BeTrue();
    }

    [Fact]
    public async Task SaveVideoAsync_ReturnedPathContainsVideoId()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();

        using var stream = new MemoryStream(new byte[] { 1 });
        var path = await _sut.SaveVideoAsync(userId, videoId, ".mp4", stream);

        path.Should().Contain(videoId.ToString());
    }

    [Fact]
    public async Task SaveVideoAsync_FileContentIsPreserved()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var content = new byte[] { 10, 20, 30 };

        using var stream = new MemoryStream(content);
        var path = await _sut.SaveVideoAsync(userId, videoId, ".mp4", stream);

        var saved = await File.ReadAllBytesAsync(path);
        saved.Should().Equal(content);
    }

    [Fact]
    public async Task SaveVideoAsync_CreatesDirectoryIfNotExists()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();

        using var stream = new MemoryStream(new byte[] { 9, 8, 7 });
        await _sut.SaveVideoAsync(userId, videoId, ".mp4", stream);

        var expectedDir = Path.Combine(_basePath, "Storage", userId.ToString(), "videos");
        Directory.Exists(expectedDir).Should().BeTrue();
    }

    [Theory]
    [InlineData(".mp4")]
    [InlineData(".avi")]
    [InlineData(".mov")]
    [InlineData(".mkv")]
    public async Task SaveVideoAsync_MultipleExtensions_AreHandled(string extension)
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();

        using var stream = new MemoryStream(new byte[] { 1, 2 });
        var path = await _sut.SaveVideoAsync(userId, videoId, extension, stream);

        path.Should().EndWith(extension);
        File.Exists(path).Should().BeTrue();
    }

    // ── GetThumbnailZipAsync ──────────────────────────────────────────────────

    [Fact]
    public async Task GetThumbnailZipAsync_FileExists_ReturnsStream()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();

        var dir = Path.Combine(_basePath, "Storage", userId.ToString(), "thumbnails");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{videoId}.zip");
        await File.WriteAllBytesAsync(filePath, new byte[] { 0x50, 0x4B, 0x03, 0x04 });

        var result = await _sut.GetThumbnailZipAsync(userId, videoId);

        result.Should().NotBeNull();
        result!.Dispose();
    }

    [Fact]
    public async Task GetThumbnailZipAsync_FileExists_StreamHasContent()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();

        var dir = Path.Combine(_basePath, "Storage", userId.ToString(), "thumbnails");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, $"{videoId}.zip");
        var data = new byte[] { 0x50, 0x4B, 0x03, 0x04 };
        await File.WriteAllBytesAsync(filePath, data);

        var result = await _sut.GetThumbnailZipAsync(userId, videoId);

        result!.Length.Should().Be(data.Length);
        result.Dispose();
    }

    [Fact]
    public async Task GetThumbnailZipAsync_FileNotExists_ReturnsNull()
    {
        var result = await _sut.GetThumbnailZipAsync(Guid.NewGuid(), Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── DeleteVideoAsync ──────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteVideoAsync_FileExists_DeletesFile()
    {
        var userId = Guid.NewGuid();
        var videoId = Guid.NewGuid();
        var filename = $"{videoId}.mp4";

        var dir = Path.Combine(_basePath, "Storage", userId.ToString(), "videos");
        Directory.CreateDirectory(dir);
        var filePath = Path.Combine(dir, filename);
        await File.WriteAllBytesAsync(filePath, new byte[] { 1, 2 });

        await _sut.DeleteVideoAsync(userId, videoId, filename);

        File.Exists(filePath).Should().BeFalse();
    }

    [Fact]
    public async Task DeleteVideoAsync_FileNotExists_DoesNotThrow()
    {
        Func<Task> act = () => _sut.DeleteVideoAsync(Guid.NewGuid(), Guid.NewGuid(), "nonexistent.mp4");

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task DeleteVideoAsync_OnlyDeletesTargetFile_LeavesOthersIntact()
    {
        var userId = Guid.NewGuid();
        var videoId1 = Guid.NewGuid();
        var videoId2 = Guid.NewGuid();
        var filename1 = $"{videoId1}.mp4";
        var filename2 = $"{videoId2}.mp4";

        var dir = Path.Combine(_basePath, "Storage", userId.ToString(), "videos");
        Directory.CreateDirectory(dir);
        var path1 = Path.Combine(dir, filename1);
        var path2 = Path.Combine(dir, filename2);
        await File.WriteAllBytesAsync(path1, new byte[] { 1 });
        await File.WriteAllBytesAsync(path2, new byte[] { 2 });

        await _sut.DeleteVideoAsync(userId, videoId1, filename1);

        File.Exists(path1).Should().BeFalse();
        File.Exists(path2).Should().BeTrue();
    }
}
