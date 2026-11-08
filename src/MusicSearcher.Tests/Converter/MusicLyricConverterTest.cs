using LyricsScraperNET.Models.Responses;
using MusicSearcher.Converter;
using MusicSearcher.Model;
using Shouldly;

namespace MusicSearcher.Tests.Converter;

public class MusicLyricConverterTest
{
    private const string LyricText = "Exit light, enter night";
    private const string ResponseMessage = "Some details";

    [Fact]
    public void ToInternal_Found_IsSuccessWithLyric()
    {
        // Arrange
        var searchResult = LyricsScraperModelFactory.SearchResult(SearchResultCode.Found, LyricText, responseMessage: ResponseMessage);

        // Act
        var result = searchResult.ToInternal();

        // Assert
        result.ShouldBeEquivalentTo(new MusicLyric
        {
            IsSuccessSearchResult = true,
            Instrumental = false,
            LyricText = LyricText,
            ResponseMessage = ResponseMessage,
        });
    }

    [Fact]
    public void ToInternal_Instrumental_IsSuccessAndInstrumental()
    {
        // Arrange
        var searchResult = LyricsScraperModelFactory.SearchResult(SearchResultCode.Instrumental, responseMessage: ResponseMessage);

        // Act
        var result = searchResult.ToInternal();

        // Assert
        result.ShouldBeEquivalentTo(new MusicLyric
        {
            IsSuccessSearchResult = true,
            Instrumental = true,
            LyricText = string.Empty,
            ResponseMessage = ResponseMessage,
        });
    }

    [Fact]
    public void ToInternal_OtherCodes_IsNotSuccess()
    {
        // Arrange
        var codes = new[]
        {
            SearchResultCode.NotFound,
            SearchResultCode.AllProvidersFailed,
            SearchResultCode.EmptyRequest,
            SearchResultCode.EmptyArtistOrSong,
            SearchResultCode.EmptyUri,
            SearchResultCode.ProviderNotAvailable,
            SearchResultCode.NoProviders,
            SearchResultCode.AllProvidersDisabled,
        };

        foreach (var code in codes)
        {
            // Act
            var result = LyricsScraperModelFactory.SearchResult(code, responseMessage: ResponseMessage).ToInternal();

            // Assert
            result.ShouldBeEquivalentTo(new MusicLyric
            {
                IsSuccessSearchResult = false,
                Instrumental = false,
                LyricText = string.Empty,
                ResponseMessage = ResponseMessage,
            });
        }
    }
}
