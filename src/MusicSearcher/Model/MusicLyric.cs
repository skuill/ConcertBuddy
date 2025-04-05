using LyricsScraperNET.Models.Responses;

namespace MusicSearcher.Model
{
    public sealed class MusicLyric
    {
        /// <summary>
        /// The text of the found lyrics. If the lyrics could not be found, an empty value is returned.
        /// </summary>
        public string LyricText { get; internal set; }

        public bool IsSuccessSearchResult { get; internal set; }

        /// <summary>
        /// A message that may contain additional information in case of problems with the search.
        /// </summary> 
        public string ResponseMessage { get; internal set; } = string.Empty;

        /// <summary>
        /// The flag indicates that the search results are for music only, without text.
        /// </summary>
        public bool Instrumental { get; internal set; } = false;

        public static MusicLyric Empty () => new MusicLyric
        {
            IsSuccessSearchResult = false
        };
    }
}
