using Hqub.MusicBrainz.API.Entities;
using IF.Lastfm.Core.Objects;

namespace MusicSearcher.Model
{
    public class MusicArtist
    {
        public Artist MusicBrainzArtist { get; set; }

        public LastArtist LastFmArtist { get; set; }

        public string Name => MusicBrainzArtist?.Name ?? LastFmArtist?.Name;

        public string MBID => MusicBrainzArtist?.Id ?? LastFmArtist?.Mbid;

        public int? Score => MusicBrainzArtist?.Score;

        public Uri ImageUri => TryGetLastFmImageUri();

        public string Biography => LastFmArtist?.Bio?.Summary;

        public Uri Url => LastFmArtist?.Url;

        // LastFM API return blank image:
        // https://support.last.fm/t/api-announcement-usage-of-audio-audiovisual-images-or-artwork/202
        private Uri TryGetLastFmImageUri()
        {
            if (LastFmArtist?.MainImage?.Small != null && !LastFmArtist.MainImage.Small.ToString().Contains("2a96cbd8b46e442fc41c2b86b821562f.png"))
            {
                return LastFmArtist.MainImage.Small;
            }
            if (LastFmArtist?.MainImage?.Medium != null && !LastFmArtist.MainImage.Medium.ToString().Contains("2a96cbd8b46e442fc41c2b86b821562f.png"))
            {
                return LastFmArtist.MainImage.Small;
            }
            if (LastFmArtist?.MainImage?.Large != null && !LastFmArtist.MainImage.Large.ToString().Contains("2a96cbd8b46e442fc41c2b86b821562f.png"))
            {
                return LastFmArtist.MainImage.Small;
            }
            if (LastFmArtist?.MainImage?.ExtraLarge != null && !LastFmArtist.MainImage.ExtraLarge.ToString().Contains("2a96cbd8b46e442fc41c2b86b821562f.png"))
            {
                return LastFmArtist.MainImage.Small;
            }
            if (LastFmArtist?.MainImage?.Mega != null && !LastFmArtist.MainImage.Mega.ToString().Contains("2a96cbd8b46e442fc41c2b86b821562f.png"))
            {
                return LastFmArtist.MainImage.Small;
            }
            return null;
        }
    }
}
