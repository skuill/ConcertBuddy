namespace MusicSearcher.Model;

public sealed class MusicLyric
{
    /// <summary>
    /// The text of the found lyrics. If the lyrics could not be found, an empty value is returned.
    /// </summary>
    public string? LyricText { get; init; }

    public bool IsSuccessSearchResult { get; init; }

    /// <summary>
    /// A message that may contain additional information in case of problems with the search.
    /// </summary> 
    public string ResponseMessage { get; init; } = string.Empty;

    /// <summary>
    /// The flag indicates that the search results are for music only, without text.
    /// </summary>
    public bool Instrumental { get; init; } = false;

    public static MusicLyric Empty() => new MusicLyric
    {
        IsSuccessSearchResult = false
    };
}
