using Hqub.MusicBrainz.API.Entities;
using IF.Lastfm.Core.Objects;
using SpotifyAPI.Web;

namespace MusicSearcher.Model
{
    public class MusicArtist
    {
        public Artist MusicBrainzArtist { get; set; }

        public LastArtist LastFmArtist { get; set; }

        public FullArtist SpotifyArtist { get; set; }

        public string Name => MusicBrainzArtist?.Name ?? LastFmArtist?.Name;

        public string MBID => MusicBrainzArtist?.Id ?? LastFmArtist?.Mbid;

        public int? Score => MusicBrainzArtist?.Score;

        public Uri ImageUri => TryGetSpotifyArtistImage() ?? TryGetLastFmArtistImageUri();

        public string Biography => LastFmArtist?.Bio?.Summary;

        public Uri Url => LastFmArtist?.Url;

        private Uri TryGetSpotifyArtistImage()
        {
            if (SpotifyArtist is null || SpotifyArtist.Images is null || !SpotifyArtist.Images.Any())
                return null;
            return new Uri(SpotifyArtist.Images.First().Url);
        }

        private const string LAST_FM_BLANK_IMAGE_NAME = "2a96cbd8b46e442fc41c2b86b821562f.png";
        // LastFM API return blank image:
        // https://support.last.fm/t/api-announcement-usage-of-audio-audiovisual-images-or-artwork/202
        private Uri TryGetLastFmArtistImageUri()
        {
            if (LastFmArtist?.MainImage?.Small != null && !LastFmArtist.MainImage.Small.ToString().Contains(LAST_FM_BLANK_IMAGE_NAME))
            {
                return LastFmArtist.MainImage.Small;
            }
            if (LastFmArtist?.MainImage?.Medium != null && !LastFmArtist.MainImage.Medium.ToString().Contains(LAST_FM_BLANK_IMAGE_NAME))
            {
                return LastFmArtist.MainImage.Small;
            }
            if (LastFmArtist?.MainImage?.Large != null && !LastFmArtist.MainImage.Large.ToString().Contains(LAST_FM_BLANK_IMAGE_NAME))
            {
                return LastFmArtist.MainImage.Small;
            }
            if (LastFmArtist?.MainImage?.ExtraLarge != null && !LastFmArtist.MainImage.ExtraLarge.ToString().Contains(LAST_FM_BLANK_IMAGE_NAME))
            {
                return LastFmArtist.MainImage.Small;
            }
            if (LastFmArtist?.MainImage?.Mega != null && !LastFmArtist.MainImage.Mega.ToString().Contains(LAST_FM_BLANK_IMAGE_NAME))
            {
                return LastFmArtist.MainImage.Small;
            }
            return null;
        }
    }
}
