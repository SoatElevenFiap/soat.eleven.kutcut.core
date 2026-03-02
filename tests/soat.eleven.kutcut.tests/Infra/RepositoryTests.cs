using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using soat.eleven.kutcut.infra.Context;
using soat.eleven.kutcut.infra.Models;
using soat.eleven.kutcut.infra.Models.Enums;
using soat.eleven.kutcut.infra.Repository;

namespace soat.eleven.kutcut.tests.Infra;

public class RepositoryTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly Repository<VideoModel> _repository;

    public RepositoryTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _repository = new Repository<VideoModel>(_context);
    }

    public void Dispose() => _context.Dispose();

    private static VideoModel CreateVideo(string title = "Test Video") => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        UserId = Guid.NewGuid(),
        Filename = "test.mp4",
        Status = StatusEnum.Pendente,
        CreatedAt = DateTime.UtcNow
    };

    // ── AddAsync ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AddAsync_ValidEntity_ReturnsAndPersistsEntity()
    {
        var video = CreateVideo();

        var result = await _repository.AddAsync(video);

        result.Should().BeSameAs(video);
        var stored = await _context.Videos.FindAsync(video.Id);
        stored.Should().NotBeNull();
        stored!.Title.Should().Be("Test Video");
    }

    // ── GetByIdAsync ──────────────────────────────────────────────────────────

    [Fact]
    public async Task GetByIdAsync_ExistingId_ReturnsEntity()
    {
        var video = CreateVideo("Find Me");
        await _repository.AddAsync(video);

        var result = await _repository.GetByIdAsync(video.Id);

        result.Should().NotBeNull();
        result!.Title.Should().Be("Find Me");
    }

    [Fact]
    public async Task GetByIdAsync_NonExistingId_ReturnsNull()
    {
        var result = await _repository.GetByIdAsync(Guid.NewGuid());

        result.Should().BeNull();
    }

    // ── GetAllAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task GetAllAsync_MultipleEntities_ReturnsAll()
    {
        await _repository.AddAsync(CreateVideo("V1"));
        await _repository.AddAsync(CreateVideo("V2"));
        await _repository.AddAsync(CreateVideo("V3"));

        var result = await _repository.GetAllAsync();

        result.Should().HaveCount(3);
    }

    [Fact]
    public async Task GetAllAsync_EmptyDatabase_ReturnsEmptyList()
    {
        var result = await _repository.GetAllAsync();

        result.Should().BeEmpty();
    }

    // ── GetPagedAsync ─────────────────────────────────────────────────────────

    [Fact]
    public async Task GetPagedAsync_ReturnsCorrectPage()
    {
        for (int i = 1; i <= 5; i++)
            await _repository.AddAsync(CreateVideo($"Video {i}"));

        var (items, totalCount) = await _repository.GetPagedAsync(1, 2);

        items.Should().HaveCount(2);
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetPagedAsync_SecondPage_ReturnsRemainingItems()
    {
        for (int i = 1; i <= 5; i++)
            await _repository.AddAsync(CreateVideo($"Video {i}"));

        var (items, totalCount) = await _repository.GetPagedAsync(2, 3);

        items.Should().HaveCount(2); // 5 total, 3 on page 1, 2 on page 2
        totalCount.Should().Be(5);
    }

    [Fact]
    public async Task GetPagedAsync_WithPredicate_FiltersCorrectly()
    {
        var userId = Guid.NewGuid();
        var v1 = CreateVideo("Match");
        v1.UserId = userId;
        await _repository.AddAsync(v1);
        await _repository.AddAsync(CreateVideo("No Match"));

        var (items, totalCount) = await _repository.GetPagedAsync(1, 10, v => v.UserId == userId);

        items.Should().HaveCount(1);
        totalCount.Should().Be(1);
        items.First().Title.Should().Be("Match");
    }

    // ── FindAsync ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task FindAsync_MatchingPredicate_ReturnsMatchingEntities()
    {
        var userId = Guid.NewGuid();
        var v1 = CreateVideo("Match 1");
        v1.UserId = userId;
        var v2 = CreateVideo("Match 2");
        v2.UserId = userId;
        await _repository.AddAsync(v1);
        await _repository.AddAsync(v2);
        await _repository.AddAsync(CreateVideo("No Match"));

        var result = await _repository.FindAsync(v => v.UserId == userId);

        result.Should().HaveCount(2);
    }

    [Fact]
    public async Task FindAsync_NoMatchingPredicate_ReturnsEmpty()
    {
        await _repository.AddAsync(CreateVideo());

        var result = await _repository.FindAsync(v => v.Title == "nonexistent");

        result.Should().BeEmpty();
    }

    // ── UpdateAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UpdateAsync_ExistingEntity_PersistsChanges()
    {
        var video = CreateVideo("Original Title");
        await _repository.AddAsync(video);

        video.Title = "Updated Title";
        await _repository.UpdateAsync(video);

        var updated = await _context.Videos.FindAsync(video.Id);
        updated!.Title.Should().Be("Updated Title");
    }

    // ── DeleteAsync ───────────────────────────────────────────────────────────

    [Fact]
    public async Task DeleteAsync_ExistingEntity_RemovesFromDatabase()
    {
        var video = CreateVideo();
        await _repository.AddAsync(video);

        await _repository.DeleteAsync(video);

        var deleted = await _context.Videos.FindAsync(video.Id);
        deleted.Should().BeNull();
    }
}
