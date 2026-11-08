using LyricsScraperNET.Models.Responses;
using MusicSearcher.Model;

namespace MusicSearcher.Converter;

internal static class MusicLyricConverter
{
    public static MusicLyric ToInternal(this SearchResult searchResult)
    {
        return new MusicLyric
        {
            Instrumental = searchResult.Instrumental,
            IsSuccessSearchResult = searchResult.Code == SearchResultCode.Found || searchResult.Code == SearchResultCode.Instrumental,
            LyricText = searchResult.LyricText,
            ResponseMessage = searchResult.ResponseMessage
        };
    }
}
