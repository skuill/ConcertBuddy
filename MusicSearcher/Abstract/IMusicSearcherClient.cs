using MusicSearcher.Model;
using MusicSearcher.MusicBrainz;

namespace MusicSearcher.Abstract
{
    public interface IMusicSearcherClient
    {
        Task<MusicArtist> SearchArtistByMBID(string mbid);

        Task<MusicArtist> SearchArtistByName(string name, ScoreType scoreType = ScoreType.MusicBrainz);

        Task<IEnumerable<MusicArtist>> SearchArtistsByName(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5);

        void WithLastFmClient(string apiKey, string secret);
    }
}
