using LyricsScraperNET.Models.Responses;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MusicSearcher.Converter;

namespace MusicSearcher.Tests.Converter;

[TestClass]
public class MusicLyricConverterTest
{
    private const string LyricText = "Exit light, enter night";
    private const string ResponseMessage = "Some details";

    public static IEnumerable<object[]> OtherCodes => new[]
    {
        new object[] { SearchResultCode.NotFound },
        new object[] { SearchResultCode.AllProvidersFailed },
        new object[] { SearchResultCode.EmptyRequest },
        new object[] { SearchResultCode.EmptyArtistOrSong },
        new object[] { SearchResultCode.EmptyUri },
        new object[] { SearchResultCode.ProviderNotAvailable },
        new object[] { SearchResultCode.NoProviders },
        new object[] { SearchResultCode.AllProvidersDisabled },
    };

    [TestMethod]
    public void ToInternal_Found_IsSuccessWithLyric()
    {
        // Arrange
        var searchResult = LyricsScraperModelFactory.SearchResult(SearchResultCode.Found, LyricText, responseMessage: ResponseMessage);

        // Act
        var result = searchResult.ToInternal();

        // Assert
        Assert.IsTrue(result.IsSuccessSearchResult);
        Assert.IsFalse(result.Instrumental);
        Assert.AreEqual(LyricText, result.LyricText);
        Assert.AreEqual(ResponseMessage, result.ResponseMessage);
    }

    [TestMethod]
    public void ToInternal_Instrumental_IsSuccessAndInstrumental()
    {
        // Arrange
        var searchResult = LyricsScraperModelFactory.SearchResult(SearchResultCode.Instrumental, responseMessage: ResponseMessage);

        // Act
        var result = searchResult.ToInternal();

        // Assert
        Assert.IsTrue(result.IsSuccessSearchResult);
        Assert.IsTrue(result.Instrumental);
        Assert.AreEqual(string.Empty, result.LyricText);
        Assert.AreEqual(ResponseMessage, result.ResponseMessage);
    }

    [DataTestMethod]
    [DynamicData(nameof(OtherCodes))]
    public void ToInternal_OtherCodes_IsNotSuccess(SearchResultCode code)
    {
        // Arrange
        var searchResult = LyricsScraperModelFactory.SearchResult(code, responseMessage: ResponseMessage);

        // Act
        var result = searchResult.ToInternal();

        // Assert
        Assert.IsFalse(result.IsSuccessSearchResult);
        Assert.IsFalse(result.Instrumental);
        Assert.AreEqual(string.Empty, result.LyricText);
        Assert.AreEqual(ResponseMessage, result.ResponseMessage);
    }
}
