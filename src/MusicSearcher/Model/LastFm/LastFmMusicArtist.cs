using Hqub.Lastfm.Entities;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;

namespace MusicSearcher.Model.LastFm
{
    public class LastFmMusicArtist : MusicArtistBase
    {
        private const string LAST_FM_BLANK_IMAGE_NAME = "2a96cbd8b46e442fc41c2b86b821562f.png";
        private Artist _artist { get; set; }

        public LastFmMusicArtist(Artist lastFmArtist)
        {
            _artist = lastFmArtist;
        }

        public override string? Name { get => _artist.Name; }
        public override string? MBID { get => _artist.MBID; }
        public override int? Score { get => default; }
        public override Uri? ImageUri { get => TryGetLastFmArtistImageUri(); }
        public override string? Biography { get => _artist.Biography?.Summary; }
        public override string? Area { get => default; }
        public override string? ActiveYears { get => default; }
        public override string? Type { get => default; }
        public override string? Country { get => default; }
        public override Uri? ExternalUrl
        {
            get => !string.IsNullOrWhiteSpace(_artist.Url)
                ? new Uri(_artist.Url)
                : null;
        }

        public override MusicServiceType MusicServiceType => MusicServiceType.LastFm;

        // LastFM API return blank image:
        // https://support.last.fm/t/api-announcement-usage-of-audio-audiovisual-images-or-artwork/202
        private Uri? TryGetLastFmArtistImageUri()
        {
            if (_artist.Images == null || !_artist.Images.Any())
                return null;

            var images = _artist.Images.Where(x => !string.IsNullOrWhiteSpace(x.Url) && !x.Url.Contains(LAST_FM_BLANK_IMAGE_NAME));

            if (!images.Any())
                return null;

            return new Uri(images.First().Url);
        }
    }
}
