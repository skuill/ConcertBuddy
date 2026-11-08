using MusicSearcher.Model;
using MusicSearcher.MusicService;
using Shouldly;

namespace MusicSearcher.ContractTests;

/// <summary>
/// Calls the real MusicBrainz service, no keys needed. The service allows about 1 request per second and may answer 503.
/// </summary>
public class MusicBrainzContractTest
{
    private readonly MusicSearcherClient _client = new();

    public MusicBrainzContractTest()
    {
        ContractTestContext.ClearMusicBrainzCache();
    }

    [Fact]
    public async Task SearchArtistByName_EmptyArtist_IsNull()
    {
        // Act
        var result = await _client.SearchArtistByName("");

        // Assert
        result.ShouldBeNull();
    }

    [Theory]
    [InlineData("Parkway Drive", "Parkway Drive")]
    [InlineData("parkway drive", "Parkway Drive")]
    public async Task SearchArtistByName_KnownArtist_ReturnsArtist(string name, string expected)
    {
        // Act
        var result = await _client.SearchArtistByName(name);

        // Assert
        result.Name.ShouldBe(expected);
    }

    [Fact(Skip = "Abbreviations don't work correctly")]
    public async Task SearchArtistByName_AbbreviatedArtist_ReturnsArtist()
    {
        // Act
        var result = await _client.SearchArtistByName("RHCP");

        // Assert
        result.Name.ShouldBe("Red Hot Chili Peppers");
    }

    [Fact]
    public async Task SearchArtistByMBID_MalformedMbid_ReturnsEmptyArtist()
    {
        // Act
        var result = (MusicArtist)await _client.SearchArtistByMBID("gafgxvbasdf23423");

        // Assert
        result[MusicServiceType.MusicBrainz].ShouldBeNull();
        result.Name.ShouldBeNullOrEmpty();
        result.Biography.ShouldBeNullOrEmpty();
        result.MBID.ShouldBeNullOrEmpty();
    }

    [Fact]
    public async Task SearchArtistByMBID_KnownMbid_ReturnsArtist()
    {
        // Act
        var result = await _client.SearchArtistByMBID("b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d");

        // Assert
        result.Name.ShouldBe("The Beatles");
    }

    [Fact]
    public async Task SearchRecordByName_KnownSong_ReturnsRecording()
    {
        // Act
        var result = await _client.SearchRecordByName("a436dd02-0549-4c91-b608-df451217fdeb", "Wishing Wells");

        // Assert
        result.ShouldNotBeNull();
        result.Id.ShouldBe("a99d7f83-75b1-46f6-8b71-cd230b7c3060");
    }
}
