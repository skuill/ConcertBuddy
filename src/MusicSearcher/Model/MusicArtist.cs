using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;
using System.Collections;

namespace MusicSearcher.Model
{
    public class MusicArtist : MusicArtistBase, IEnumerable<MusicArtistBase>
    {
        private List<MusicArtistBase> _musicArtists;

        // Indexer with only a get accessor with the expression-bodied definition:
        public MusicArtistBase this[MusicServiceType musicServiceType] => GetMusicArtistByServiceType(musicServiceType);

        public MusicArtist()
        {
            _musicArtists = new List<MusicArtistBase>();
        }

        public MusicArtist(MusicArtistBase musicArtist)
        {
            _musicArtists = new List<MusicArtistBase> { musicArtist };
        }

        public MusicArtist(List<MusicArtistBase> musicArtists)
        {
            _musicArtists = musicArtists;
        }

        public override MusicServiceType MusicServiceType => MusicServiceType.None;

        public override string? Name => GetFirstNonDefaultValue(x => x.Name);

        public override string? MBID => GetFirstNonDefaultValue(x => x.MBID);

        public override int? Score => GetFirstNonDefaultValue(x => x.Score);

        public override Uri? ImageUri => GetFirstNonDefaultValue(x => x.ImageUri);

        public override string? Biography => GetFirstNonDefaultValue(x => x.Biography);

        public override string? Area => GetFirstNonDefaultValue(x => x.Area);

        public override string? ActiveYears => GetFirstNonDefaultValue(x => x.ActiveYears);

        public override string? Type => GetFirstNonDefaultValue(x => x.Type);

        public override string? Country => GetFirstNonDefaultValue(x => x.Country);

        public override Uri? ExternalUrl => GetFirstNonDefaultValue(x => x.ExternalUrl);

        private T? GetFirstNonDefaultValue<T>(Func<MusicArtistBase, T> selector)
        {
            return _musicArtists.Select(selector).FirstOrDefault(x => !EqualityComparer<T>.Default.Equals(x, default));
        }

        public void Add(MusicArtistBase artist)
        {
            if (!_musicArtists.Contains(artist))
                _musicArtists.Add(artist);
            else
                throw new InvalidOperationException($"Try to add already exist artist {artist.MusicServiceType}");
        }

        public override bool IsMusicArtistExist(MusicServiceType musicServiceType)
        {
            return _musicArtists != null && _musicArtists.Any(x => x.MusicServiceType == musicServiceType);
        }

        public override MusicArtistBase? GetMusicArtistByServiceType(MusicServiceType musicServiceType)
        {
            return _musicArtists?.FirstOrDefault(x => x.MusicServiceType == musicServiceType);
        }

        public IEnumerator<MusicArtistBase> GetEnumerator()
        {
            return _musicArtists.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
