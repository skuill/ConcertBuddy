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

        public MusicArtist() {
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

        public override string Name { get => _musicArtists?.Select(x => x.Name).Where(x => x != default).FirstOrDefault(); }

        public override string MBID { get => _musicArtists?.Select(x => x.MBID).Where(x => x != default).FirstOrDefault(); }

        public override int? Score { get => _musicArtists?.Select(x => x.Score).Where(x => x != default).FirstOrDefault(); }

        public override Uri ImageUri { get => _musicArtists?.Select(x => x.ImageUri).Where(x => x != default).FirstOrDefault(); }

        public override string Biography { get => _musicArtists?.Select(x => x.Biography).Where(x => x != default).FirstOrDefault(); }

        /// <summary>
        /// Areas are geographic regions or settlements.
        /// </summary>
        public override string Area { get => _musicArtists?.Select(x => x.Area).Where(x => x != default).FirstOrDefault(); }

        public override string ActiveYears { get => _musicArtists?.Select(x => x.ActiveYears).Where(x => x != default).FirstOrDefault(); }

        public override string Type { get => _musicArtists?.Select(x => x.Type).Where(x => x != default).FirstOrDefault(); }

        public override string Country { get => _musicArtists?.Select(x => x.Country).Where(x => x != default).FirstOrDefault(); }

        public override Uri ExternalUrl { get => _musicArtists?.Select(x => x.ExternalUrl).Where(x => x != default).FirstOrDefault(); }

        public override MusicServiceType MusicServiceType => MusicServiceType.None;

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

        public override MusicArtistBase GetMusicArtistByServiceType(MusicServiceType musicServiceType)
        {
            if (_musicArtists == null)
                return null;

            return _musicArtists.FirstOrDefault(x => x.MusicServiceType == musicServiceType);
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
