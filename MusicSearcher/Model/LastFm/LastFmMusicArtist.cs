using IF.Lastfm.Core.Objects;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;

namespace MusicSearcher.Model.LastFm
{
    public class LastFmMusicArtist : MusicArtistBase
    {
        private const string LAST_FM_BLANK_IMAGE_NAME = "2a96cbd8b46e442fc41c2b86b821562f.png";
        private LastArtist _artist { get; set; }

        public LastFmMusicArtist(LastArtist lastFmArtist)
        {
            _artist = lastFmArtist;
        }

        public override string Name { get => _artist.Name; }
        public override string MBID { get => _artist.Mbid; }
        public override int? Score { get => default; }
        public override Uri ImageUri { get => TryGetLastFmArtistImageUri(); }
        public override string Biography { get => _artist.Bio?.Summary; }
        public override string Area { get => default; }
        public override string ActiveYears { get => default; }
        public override string Type { get => default; }
        public override string Country { get => default; }
        public override Uri ExternalUrl { get => _artist.Url; }

        public override MusicServiceType MusicServiceType => MusicServiceType.LastFm;

        // LastFM API return blank image:
        // https://support.last.fm/t/api-announcement-usage-of-audio-audiovisual-images-or-artwork/202
        private Uri TryGetLastFmArtistImageUri()
        {
            if (_artist?.MainImage?.Small != null && !_artist.MainImage.Small.ToString().Contains(LAST_FM_BLANK_IMAGE_NAME))
            {
                return _artist.MainImage.Small;
            }
            if (_artist?.MainImage?.Medium != null && !_artist.MainImage.Medium.ToString().Contains(LAST_FM_BLANK_IMAGE_NAME))
            {
                return _artist.MainImage.Small;
            }
            if (_artist?.MainImage?.Large != null && !_artist.MainImage.Large.ToString().Contains(LAST_FM_BLANK_IMAGE_NAME))
            {
                return _artist.MainImage.Small;
            }
            if (_artist?.MainImage?.ExtraLarge != null && !_artist.MainImage.ExtraLarge.ToString().Contains(LAST_FM_BLANK_IMAGE_NAME))
            {
                return _artist.MainImage.Small;
            }
            if (_artist?.MainImage?.Mega != null && !_artist.MainImage.Mega.ToString().Contains(LAST_FM_BLANK_IMAGE_NAME))
            {
                return _artist.MainImage.Small;
            }
            return null;
        }

        public override MusicArtistBase GetMusicArtistByServiceType(MusicServiceType musicServiceType)
        {
            if (MusicServiceType == musicServiceType)
                return this;
            return null;
        }

        public override bool IsMusicArtistExist(MusicServiceType musicServiceType)
        {
            return MusicServiceType == musicServiceType;
        }
    }
}
