using FakeItEasy;
using Hqub.MusicBrainz;
using LyricsScraperNET;
using Microsoft.Extensions.Logging;
using MusicSearcher.Model;
using MusicSearcher.MusicService;
using MusicSearcher.MusicService.LastFm;
using MusicSearcher.MusicService.SetlistFm;
using Shouldly;

namespace MusicSearcher.ContractTests;

/// <summary>
/// Calls the real Last.fm and MusicBrainz services. Skipped when the Last.fm keys are not set.
/// </summary>
public class LastFmContractTest
{
    public LastFmContractTest()
    {
        ContractTestContext.ClearMusicBrainzCache();
    }

    [Fact]
    public async Task SearchArtistByMBID_KnownMbid_HasLastFmArtist()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var result = (MusicArtist)await client.SearchArtistByMBID("b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d");

        // Assert
        result.Name.ShouldBe("The Beatles");
        result[MusicServiceType.LastFm].ShouldNotBeNull().Name.ShouldBe("The Beatles");
    }

    [Fact]
    public async Task SearchArtistByMBID_MbidUnknownToLastFm_FoundByName()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var result = (MusicArtist)await client.SearchArtistByMBID("7f233cda-eacb-4235-b681-5f7be343a1a2");

        // Assert
        result[MusicServiceType.LastFm].ShouldNotBeNull().Name.ShouldBe("Rosé");
    }

    [Fact]
    public async Task SearchTopTracks_KnownMbid_ReturnsTenTracks()
    {
        // Arrange
        var client = CreateClient();

        // Act
        var result = (await client.SearchTopTracks("65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab")).ToList();

        // Assert
        result.Count.ShouldBe(10);
        result.Select(x => x.TrackName).ShouldContain("Enter Sandman");
    }

    private static MusicSearcherClient CreateClient()
    {
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(ContractTestContext.LastFmApiKey) || string.IsNullOrWhiteSpace(ContractTestContext.LastFmApiSecret),
            "Last.fm keys are not set");

        return new MusicSearcherClient(
            A.Fake<ILyricsScraperClient>(),
            A.Fake<ISetlistFmServiceClient>(),
            new[] { new LastFmServiceClient(ContractTestContext.LastFmApiKey!, ContractTestContext.LastFmApiSecret!) },
            new MusicBrainzClient(),
            A.Fake<ILogger<MusicSearcherClient>>());
    }
}
