using FakeItEasy;
using LyricsScraperNET;
using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicSearcher.Model;
using MusicSearcher.MusicService;
using MusicSearcher.MusicService.LastFm;
using MusicSearcher.MusicService.SetlistFm;
using MusicSearcher.MusicService.Yandex;

namespace MusicSearcher.Tests;

[TestClass]
public class MusicSearcherClientTest
{
    [TestMethod]
    public async Task SearchArtistByName_EmptyArtist_IsNull()
    {
        // Arrange
        string artist = "";
        var client = InitClient();

        // Act
        var result = await client.SearchArtistByName(artist);

        // Assert
        Assert.IsNull(result);
    }

    [TestMethod]
    public async Task SearchArtistByName_DefaultExample_AreEqual()
    {
        // Arrange
        string artist = "Parkway Drive";
        var client = InitClient();

        // Act
        var result = await client.SearchArtistByName(artist);

        // Assert
        Assert.IsTrue(string.Equals(artist, result.Name));
    }

    [TestMethod]
    public async Task SearchArtistByName_LowercaseArtist_AreEqual()
    {
        // Arrange
        string artist = "parkway drive";
        string expected = "Parkway Drive";
        var client = InitClient();

        // Act
        var result = await client.SearchArtistByName(artist);

        // Assert
        Assert.IsTrue(string.Equals(expected, result.Name));
    }

    [TestMethod]
    [Ignore("abbreviations don't work correctly")]
    public async Task SearchArtistByName_AbbreviatedArtist_AreEqual()
    {
        // Arrange
        string artist = "RHCP";
        string expected = "Red Hot Chili Peppers";
        var client = InitClient();

        // Act
        var result = await client.SearchArtistByName(artist);

        // Assert
        Assert.IsTrue(string.Equals(expected, result.Name));
    }

    [TestMethod]
    public async Task SearchArtistByMBID_MalformedMbid_IsNull()
    {
        // Arrange
        string mbid = "gafgxvbasdf23423";
        var client = InitClient();

        // Act
        var result = await client.SearchArtistByMBID(mbid) as MusicArtist;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNull(result[MusicServiceType.MusicBrainz]);
        Assert.IsNull(result[MusicServiceType.LastFm]);
        Assert.IsTrue(string.IsNullOrEmpty(result.Name));
        Assert.IsTrue(string.IsNullOrEmpty(result.Biography));
        Assert.IsTrue(string.IsNullOrEmpty(result.MBID));
    }

    [TestMethod]
    public async Task SearchArtistByMBID_DefaultExample_AreEqual()
    {
        // Arrange
        string artist = "The Beatles";
        string mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";
        var client = InitClient();

        // Act
        var result = await client.SearchArtistByMBID(mbid);

        // Assert
        Assert.IsTrue(string.Equals(artist, result.Name));
    }

    [TestMethod]
    public async Task SearchArtistByMBID_WithLastFmClient_AreEqual()
    {
        // Arrange
        string artist = "The Beatles";
        string mbid = "b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d";
        var client = InitClient();

        // Act
        var result = await client.SearchArtistByMBID(mbid) as MusicArtist;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result[MusicServiceType.LastFm]);
        Assert.IsTrue(string.Equals(artist, result.Name));
        Assert.IsTrue(string.Equals(artist, result[MusicServiceType.LastFm].Name));
    }

    [TestMethod]
    public async Task SearchArtistByMBID_WithLastFmClient_WithNotSpecifiedMBID_ShouldBeFound()
    {
        // Arrange
        string artist = "Rosé";
        string mbid = "7f233cda-eacb-4235-b681-5f7be343a1a2";
        var client = InitClient();

        // Act
        var result = await client.SearchArtistByMBID(mbid) as MusicArtist;

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result[MusicServiceType.LastFm]);
        Assert.IsTrue(string.Equals(artist, result[MusicServiceType.LastFm].Name));
    }

    [TestMethod]
    public async Task SearchRecordByName_DefaultExample_AreEqual()
    {
        // Arrange
        string artistMBID = "a436dd02-0549-4c91-b608-df451217fdeb"; // Artist name: Parkway Drive
        string songName = "Wishing Wells";

        var expectedRecordingMBID = "a99d7f83-75b1-46f6-8b71-cd230b7c3060";

        var client = InitClient();

        // Act
        var result = await client.SearchRecordByName(artistMBID, songName);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsTrue(string.Equals(expectedRecordingMBID, result.Id));
    }

    private IMusicSearcherClient InitClient()
    {
        var lyricsScraperClientMock = A.Fake<ILyricsScraperClient>();
        var setlistApiMock = A.Fake<ISetlistFmServiceClient>();
        var loggerMock = A.Fake<ILogger<MusicSearcherClient>>();
        YandexServiceClient yandexServiceClient = new YandexServiceClient(Configuration.YandexToken);
        LastFmServiceClient lastFmServiceClient = new LastFmServiceClient(Configuration.LastFmApiKey, Configuration.LastFmApiSecret);

        return new MusicSearcherClient(
            lyricsScraperClientMock,
            setlistApiMock,
            yandexServiceClient,
            lastFmServiceClient,
            loggerMock);
    }
}