using MusicSearcher.MusicBrainz;

namespace MusicSearcher.Abstract
{
    public interface IMusicSearcherClient
    {
        Task<string> SearchArtist(string name, ScoreType scoreType = ScoreType.MusicBrainz);

        Task<IEnumerable<string>> SearchArtists(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5);

        Task<IDictionary<string, int>> SearchArtistsWithScore(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5);
    }
}
