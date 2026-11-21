using Microsoft.Extensions.Time.Testing;
using MusicSearcher.MusicBrainz;
using Shouldly;
using System.Text;

namespace MusicSearcher.Tests.MusicBrainz;

public class MemoryRequestCacheTest
{
    private const string Request = "artist?query=artist:%22metallica%22&fmt=json&limit=5";
    private const string Answer = """{"count":0,"offset":0,"artists":[]}""";

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task TryGetCachedItem_Added_SameAnswerAndTheOriginalStreamIsReadable()
    {
        // Arrange
        using var cache = new MemoryRequestCache(_time);
        using var response = new MemoryStream(Encoding.UTF8.GetBytes(Answer));

        // Act
        await cache.Add(Request, response);
        var found = await cache.TryGetCachedItem(Request, out var stream);

        // Assert
        found.ShouldBeTrue();
        (await new StreamReader(stream).ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(Answer);
        (await cache.Contains(Request)).ShouldBeTrue();
        // The client reads the answer itself after it is cached.
        (await new StreamReader(response).ReadToEndAsync(TestContext.Current.CancellationToken)).ShouldBe(Answer);
    }

    [Fact]
    public async Task TryGetCachedItem_OtherRequest_NotFound()
    {
        // Arrange
        using var cache = new MemoryRequestCache(_time);
        await cache.Add(Request, new MemoryStream(Encoding.UTF8.GetBytes(Answer)));

        // Act
        var found = await cache.TryGetCachedItem("artist?query=artist:%22muse%22&fmt=json&limit=5", out var stream);

        // Assert
        found.ShouldBeFalse();
        stream.ShouldBeNull();
        (await cache.Contains("artist/65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab")).ShouldBeFalse();
    }

    [Fact]
    public async Task TryGetCachedItem_ADayLater_NotFoundAnyMore()
    {
        // Arrange
        using var cache = new MemoryRequestCache(_time);
        await cache.Add(Request, new MemoryStream(Encoding.UTF8.GetBytes(Answer)));
        _time.Advance(TimeSpan.FromDays(1) - TimeSpan.FromSeconds(1));
        var foundBefore = await cache.TryGetCachedItem(Request, out _);

        // Act
        _time.Advance(TimeSpan.FromSeconds(2));
        var foundAfter = await cache.TryGetCachedItem(Request, out _);

        // Assert
        foundBefore.ShouldBeTrue();
        foundAfter.ShouldBeFalse();
    }

    [Fact]
    public async Task Add_OverTheSizeLimit_NotKeptAndTheOlderAnswerStays()
    {
        // Arrange: room for one answer only.
        using var cache = new MemoryRequestCache(_time, sizeLimitBytes: Answer.Length + 10);
        await cache.Add(Request, new MemoryStream(Encoding.UTF8.GetBytes(Answer)));

        // Act
        await cache.Add("artist/65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab", new MemoryStream(Encoding.UTF8.GetBytes(Answer)));

        // Assert
        (await cache.Contains(Request)).ShouldBeTrue();
        (await cache.Contains("artist/65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab")).ShouldBeFalse();
    }
}
