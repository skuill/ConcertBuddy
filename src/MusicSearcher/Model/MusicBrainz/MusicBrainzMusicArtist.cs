using Hqub.MusicBrainz.Entities;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;

namespace MusicSearcher.Model.MusicBrainz
{
    public class MusicBrainzMusicArtist : MusicArtistBase
    {
        private Artist _artist { get; set; }

        public MusicBrainzMusicArtist(Artist musicBrainzArtist)
        {
            _artist = musicBrainzArtist;
        }

        //#28 Some hyphen from MusicBrainz are the wrong hyphen.
        // Replace Unicode: (U+2010) to Unicode: (U+002D)
        public override string Name { get => _artist.Name?.Replace("‐", "-"); }
        public override string MBID { get => _artist.Id; }
        public override int? Score { get => _artist.Score; }
        public override Uri ImageUri { get => default; }
        public override string Biography { get => default; }
        public override string Area { get => _artist.Area?.Name; }
        public override string ActiveYears { get => $"{_artist?.LifeSpan?.Begin} - {_artist?.LifeSpan?.End}"; }
        public override string Type { get => _artist.Type; }
        public override string Country { get => _artist.Country; }
        public override Uri ExternalUrl { get => default; }

        public override MusicServiceType MusicServiceType => MusicServiceType.MusicBrainz;

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
