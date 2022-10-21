using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;
using SpotifyAPI.Web;

namespace MusicSearcher.Model.Spotify
{
    internal class SpotifyMusicArtist : MusicArtistBase
    {
        private string _externalUrlKey = "spotify";

        private FullArtist _artist { get; set; }

        public SpotifyMusicArtist(FullArtist spotifyArtist)
        {
            _artist = spotifyArtist;
        }


        public FullArtist Artist => _artist;
        public override string Name { get => _artist.Name; }
        public override string MBID { get => default; }
        public override int? Score { get => default; }
        public override Uri ImageUri { get => TryGetSpotifyArtistImage(); }
        public override string Biography { get => default; }
        public override string Area { get => default; }
        public override string ActiveYears { get => default; }
        public override string Type { get => default; }
        public override string Country { get => default; }
        public override Uri ExternalUrl
        {
            get => _artist?.ExternalUrls != null
                        && _artist.ExternalUrls.TryGetValue(_externalUrlKey, out string uriString) 
                    ? new Uri(uriString) 
                    : null;
        }

        public override MusicServiceType MusicServiceType => MusicServiceType.Spotify;

        private Uri TryGetSpotifyArtistImage()
        {
            if (_artist.Images is null || !_artist.Images.Any())
                return null;
            return new Uri(_artist.Images.First().Url);
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
