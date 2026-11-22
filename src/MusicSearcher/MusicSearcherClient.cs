using Hqub.MusicBrainz;
using Hqub.MusicBrainz.Entities;
using LyricsScraperNET;
using LyricsScraperNET.Models.Requests;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using MusicSearcher.Converter;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.MusicBrainz;
using MusicSearcher.MusicBrainz;
using MusicSearcher.MusicService;
using MusicSearcher.MusicService.Abstract;
using MusicSearcher.MusicService.SetlistFm;
using System.Net;

namespace MusicSearcher;

public class MusicSearcherClient : IMusicSearcherClient
{
    private readonly ILogger<MusicSearcherClient>? _logger;

    private List<IMusicServiceClient> _musicServiceClients;

    private MusicBrainzClient _musicBrainzClient;

    // Every answer of MusicBrainz for a day: the search, the artist by MBID, the recordings of a track.
    private readonly MemoryRequestCache _musicBrainzCache = new(TimeProvider.System);

    private const int MEMORY_CACHE_SIZE = 256;
    private IMemoryCache? _artistMemoryCache;
    private readonly MemoryCacheEntryOptions _memoryCacheEntryOptions;

    private readonly ILyricsScraperClient _lyricsScraperClient = null!;

    private readonly ISetlistFmServiceClient _setlistFmClient = null!;

    private bool _isMemoryCacheEnabled;
    public bool IsMemoryCacheEnabled => _isMemoryCacheEnabled;

    public MusicSearcherClient()
        : this(new MusicBrainzClient())
    {
    }

    private MusicSearcherClient(MusicBrainzClient musicBrainzClient)
    {
        _isMemoryCacheEnabled = false;
        _memoryCacheEntryOptions = new MemoryCacheEntryOptions
        {
            AbsoluteExpirationRelativeToNow = TimeSpan.FromMinutes(10),
            SlidingExpiration = TimeSpan.FromMinutes(5),
            Size = 1
        };

        _musicBrainzClient = musicBrainzClient;
        _musicBrainzClient.Cache = _musicBrainzCache;

        _musicServiceClients = new List<IMusicServiceClient>();
    }

    public MusicSearcherClient(
        ILyricsScraperClient lyricsScraperClient,
        ISetlistFmServiceClient setlistFmClient,
        IEnumerable<IMusicServiceClient> musicServiceClients,
        MusicBrainzClient musicBrainzClient,
        ILogger<MusicSearcherClient> logger)
        : this(musicBrainzClient)
    {
        _logger = logger;
        _lyricsScraperClient = lyricsScraperClient;
        _setlistFmClient = setlistFmClient;

        _musicServiceClients.AddRange(musicServiceClients);

        _logger?.LogInformation("Enable memory cache for artist search with size {CacheSize}", MEMORY_CACHE_SIZE);
        _isMemoryCacheEnabled = true;
        var memoryCacheOptions = new MemoryCacheOptions { SizeLimit = MEMORY_CACHE_SIZE };
        _artistMemoryCache = new MemoryCache(memoryCacheOptions);
    }

    // TODO: Add additional check for aliases in case of abbreviations. For example: RHCP
    public async Task<MusicArtistBase> SearchArtistByName(string name, ScoreType scoreType = ScoreType.MusicBrainz)
    {
        return (await SearchArtistsByName(name, scoreType, 5)).FirstOrDefault()!;
    }

    // TODO: Add additional check for aliases in case of abbreviations. For example: RHCP
    /// <inheritdoc />
    public async Task<IEnumerable<MusicArtistBase>> SearchArtistsByName(string name, ScoreType scoreType = ScoreType.MusicBrainz, int limit = 5, int offset = 0)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return new List<MusicArtist>();
        }

        // In turn (#91): MusicBrainz, then Last.fm, then MusicBrainz once more. No state between the searches: every
        // search starts with MusicBrainz, which has the country, type and years that Last.fm does not have.
        try
        {
            return await SearchMusicBrainzArtistsAsync(name, scoreType, limit, offset);
        }
        catch (Exception ex)
        {
            // Warning, not Error: the user gets the artists of Last.fm or "Try again", the bot itself is fine.
            _logger?.LogWarning(ex, "MusicBrainz did not answer the artist search [{Name}]", name);
            var lastFmArtists = await SearchLastFmArtistsAsync(name, limit, offset);
            if (lastFmArtists != null)
            {
                return lastFmArtists;
            }
        }

        try
        {
            return await SearchMusicBrainzArtistsAsync(name, scoreType, limit, offset);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "MusicBrainz did not answer the artist search [{Name}] again", name);
            throw new MusicServiceUnavailableException("MusicBrainz", ex);
        }
    }

    private async Task<IEnumerable<MusicArtistBase>> SearchMusicBrainzArtistsAsync(string name, ScoreType scoreType, int limit, int offset)
    {
        // By default, search results will be ordered by score, so to get the
        // best match you could do artists.Items.First(). Sometimes this method
        // won't work (example: search for 'U2').
        var artists = await _musicBrainzClient.Artists.SearchAsync(name.Quote(), limit, offset);
        if (artists == null || !artists.Any())
        {
            _logger?.LogInformation("Can't find artist [{Name}] with search limit {Limit}", name, limit);
            return new List<MusicArtist>();
        }

        var result = scoreType switch
        {
            ScoreType.MusicBrainz => artists.Select(x => new MusicArtist(new MusicBrainzMusicArtist(x))).ToList(),
            ScoreType.Levenshtein => artists.OrderByDescending(x => Levenshtein.Similarity(x.Name, name)).Select(x => new MusicArtist(new MusicBrainzMusicArtist(x))).ToList(),
            _ => new List<MusicArtist>()
        };

        _logger?.LogDebug("Find artists for [{Name}]: {Artists}", name, string.Join("; ", result.Select(x => $"{x.Name} [score: {x.Score}]")));
        return result;
    }

    /// <summary>
    /// The search when MusicBrainz does not answer (#91): Last.fm artists with an MBID, because the card, the setlists and
    /// the buttons go by MBID. Null when there is no Last.fm, it fails too or has no such artist: then MusicBrainz is busy
    /// for the user, not "nothing found".
    /// </summary>
    private async Task<IReadOnlyList<MusicArtistBase>?> SearchLastFmArtistsAsync(string name, int limit, int offset)
    {
        var lastFm = _musicServiceClients.FirstOrDefault(x => x.MusicServiceType == MusicServiceType.LastFm);
        if (lastFm == null)
        {
            return null;
        }

        try
        {
            var artists = (await lastFm.SearchArtistsByName(name, limit, offset))
                .Where(x => !string.IsNullOrWhiteSpace(x.MBID))
                .Select(x => new MusicArtist(x))
                .ToList<MusicArtistBase>();
            // Only the count: the search text is written at the Debug level only.
            _logger?.LogInformation("MusicBrainz is busy: {Count} artists of the search from Last.fm", artists.Count);
            return artists.Count > 0 ? artists : null;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Last.fm did not answer the artist search either");
            return null;
        }
    }

    /// <summary>
    /// The card when MusicBrainz does not answer (#91): what Last.fm knows by the MBID (name, photo, biography, link), without
    /// the type, area and years of MusicBrainz. Null when there is no Last.fm, it fails too or does not know the MBID.
    /// </summary>
    private async Task<MusicArtistBase?> GetLastFmArtistAsync(string artistMBID)
    {
        var lastFm = _musicServiceClients.FirstOrDefault(x => x.MusicServiceType == MusicServiceType.LastFm);
        if (lastFm == null)
        {
            return null;
        }

        try
        {
            var artist = await lastFm.GetArtistByMBID(artistMBID);
            if (string.IsNullOrWhiteSpace(artist.Name))
            {
                return null;
            }
            _logger?.LogInformation("MusicBrainz is busy: the artist [{ArtistMBID}] from Last.fm", artistMBID);
            return new MusicArtist(artist);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Last.fm did not answer the artist [{ArtistMBID}] either", artistMBID);
            return null;
        }
    }

    public async Task<MusicArtistBase> SearchArtistByMBID(string artistMBID)
    {
        if (IsMemoryCacheEnabled && _artistMemoryCache!.TryGetValue(artistMBID, out MusicArtist? cached))
        {
            return cached!;
        }

        // In turn (#91): MusicBrainz, then Last.fm, then MusicBrainz once more.
        Artist? musicBrainzArtist;
        try
        {
            musicBrainzArtist = await GetMusicBrainzArtistAsync(artistMBID);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "MusicBrainz did not answer the artist [{ArtistMBID}]", artistMBID);
            // Not cached: the next tap gets the full card again once MusicBrainz answers.
            var lastFmArtist = await GetLastFmArtistAsync(artistMBID);
            if (lastFmArtist != null)
            {
                return lastFmArtist;
            }

            try
            {
                musicBrainzArtist = await GetMusicBrainzArtistAsync(artistMBID);
            }
            catch (Exception again)
            {
                _logger?.LogWarning(again, "MusicBrainz did not answer the artist [{ArtistMBID}] again", artistMBID);
                throw new MusicServiceUnavailableException("MusicBrainz", again);
            }
        }

        if (musicBrainzArtist == null)
        {
            return new MusicArtist();
        }

        var result = new MusicArtist();
        result.Add(new MusicBrainzMusicArtist(musicBrainzArtist));

        // The other services are asked one by one and skipped on errors.
        foreach (var musicServiceClient in _musicServiceClients.OrderByDescending(x => x.GetAvailableSearch()))
        {
            var currentSearchType = musicServiceClient.GetAvailableSearch();

            var currentSearchResult = await TrySearchArtist(musicServiceClient, currentSearchType, artistMBID, result.Name!);

            // Try search artist by name if failed for All/MBID.
            if (!currentSearchResult.Success && currentSearchType.HasFlag(AvailableSearchType.All))
            {
                currentSearchResult = await TrySearchArtist(musicServiceClient, AvailableSearchType.Name, artistMBID, result.Name!);
            }

            if (currentSearchResult.Success)
            {
                result.Add(currentSearchResult.MusicArtist!);
            }
        }

        if (IsMemoryCacheEnabled)
            _artistMemoryCache!.Set(artistMBID, result, _memoryCacheEntryOptions);

        return result;
    }

    /// <summary>
    /// Null when MusicBrainz has no such artist: a <c>400</c> or <c>404</c>, not an error.
    /// </summary>
    private async Task<Artist?> GetMusicBrainzArtistAsync(string artistMBID)
    {
        try
        {
            return await _musicBrainzClient.Artists.GetAsync(artistMBID);
        }
        catch (WebServiceException ex) when (ex.StatusCode is HttpStatusCode.BadRequest or HttpStatusCode.NotFound)
        {
            _logger?.LogInformation("MusicBrainz has no artist with mbid [{ArtistMBID}]: {StatusCode}", artistMBID, ex.StatusCode);
            return null;
        }
    }

    private async Task<(bool Success, MusicArtistBase? MusicArtist)> TrySearchArtist(
        IMusicServiceClient client,
        AvailableSearchType searchType,
        string artistMBID,
        string artistName)
    {
        MusicArtistBase? result = null;
        try
        {
            switch (searchType)
            {
                case AvailableSearchType.MBID:
                case AvailableSearchType.All:
                    result = await client.GetArtistByMBID(artistMBID);
                    break;
                case AvailableSearchType.Name:
                    result = await client.SearchArtistByName(artistName);
                    break;
            }
        }
        catch (NotImplementedException)
        {
            return (false, null);
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Can't get artist by [{SearchType}] from music service [{Client}]. Artist name: [{ArtistName}], mbid: [{ArtistMBID}]", searchType, client.GetType().Name, artistName, artistMBID);
            return (false, null);
        }
        return (true, result);
    }

    public async Task<MusicTrackBase> SearchTrack(string artistName, string trackName)
    {
        MusicTrack result = new MusicTrack();

        foreach (var musicServiceClient in _musicServiceClients)
        {
            try
            {
                var track = await musicServiceClient.SearchTrack(artistName, trackName);
                if (track == null)
                {
                    // A stage solo or a jam is no track of a music service: Information, not an error.
                    _logger?.LogInformation("Music service [{Client}] has no track [{ArtistName} - {TrackName}]", musicServiceClient.GetType().Name, artistName, trackName);
                    continue;
                }
                result.Add(track);
            }
            catch (NotImplementedException)
            {
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Can't get track from music service [{Client}].", musicServiceClient.GetType().Name);
            }
        }
        return result;
    }

    /// <inheritdoc />
    public async Task<MusicRecording?> SearchRecordByName(string artistMBID, string recordingName)
    {
        try
        {
            QueryParameters<Recording> query = new QueryParameters<Recording>();
            query.Add("arid", artistMBID);
            query.Add("recording", recordingName);

            var recordings = await _musicBrainzClient.Recordings.SearchAsync(query);

            if (recordings == null || !recordings.Any())
            {
                _logger?.LogInformation("Can't find recording for artist mbid [{ArtistMBID}] and song name [{RecordingName}]", artistMBID, recordingName);
                return null;
            }

            _logger?.LogDebug("Find recording for artist [{ArtistMBID}] with song name [{RecordingName}]", artistMBID, recordingName);
            return recordings.First().ToInternal();
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Can't find recording for artist mbid [{ArtistMBID}] and song name [{RecordingName}]", artistMBID, recordingName);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<IEnumerable<MusicTrackBase>> SearchTopTracks(string artistMBID)
    {
        List<MusicTrackBase> result = new List<MusicTrackBase>();

        var artist = await SearchArtistByMBID(artistMBID);

        foreach (var musicServiceClient in _musicServiceClients)
        {
            try
            {
                result = await musicServiceClient.SearchTopTracks(artist);
                if (result != null && result.Any())
                    return result;
            }
            catch (NotImplementedException)
            {
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Can't get top tracks from music service [{Client}]", musicServiceClient.GetType().Name);
            }
        }
        return result!;
    }

    /// <inheritdoc />
    public async Task<MusicLyric> SearchLyric(string artistName, string songName)
    {
        var searchRequest = new ArtistAndSongSearchRequest(artistName, songName);

        var searchResult = await _lyricsScraperClient.SearchLyricAsync(searchRequest);

        var lyric = searchResult?.ToInternal() ?? MusicLyric.Empty();
        if (!lyric.IsSuccessSearchResult)
        {
            _logger?.LogWarning("Can't find lyric for track [{ArtistName} - {SongName}]. Reason: [{SearchResultCode}]", artistName, songName, searchResult?.Code);
        }

        return lyric;
    }

    public async Task<MusicSetlists?> SearchArtistSetlists(string artistMBID, int page = 1)
    {
        return await _setlistFmClient.SearchArtistSetlists(artistMBID, page: page);
    }

    public async Task<MusicSetlist?> SearchSetlist(string setlistId)
    {
        return await _setlistFmClient.SearchSetlist(setlistId);
    }

    public void Dispose()
    {
        _musicBrainzCache.Dispose();
        if (_isMemoryCacheEnabled)
            _artistMemoryCache!.Dispose();
        if (_musicServiceClients != null && _musicServiceClients.Any())
        {
            foreach (var musicServiceClient in _musicServiceClients)
            {
                if (musicServiceClient is IAsyncDisposable)
                    ((IAsyncDisposable)musicServiceClient).DisposeAsync().GetAwaiter().GetResult();
            }
            _musicServiceClients.Clear();
        }
    }
}
