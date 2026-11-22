using Hqub.Lastfm;
using Hqub.Lastfm.Entities;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.Model.LastFm;
using MusicSearcher.MusicService.Abstract;

namespace MusicSearcher.MusicService.LastFm;

public sealed class LastFmServiceClient : IMusicServiceClient
{
    private const int TOP_TRACKS_LIMIT = 10;

    /// <summary>
    /// Hqub.Lastfm takes no HttpClient and no timeout: a call that does not end in this time is given up (#91). Its
    /// request may still end in the background.
    /// </summary>
    internal static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(4);

    private readonly ILastFmApi _lastFmApi;
    private readonly TimeProvider _timeProvider;
    private readonly TimeSpan _timeout;
    private AvailableSearchType availableSearch = AvailableSearchType.All;

    public LastFmServiceClient(string apiKey, string secret)
        : this(new LastFmApi(apiKey, secret))
    {
    }

    public LastFmServiceClient(ILastFmApi lastFmApi, TimeProvider? timeProvider = null, TimeSpan? timeout = null)
    {
        _lastFmApi = lastFmApi;
        _timeProvider = timeProvider ?? TimeProvider.System;
        _timeout = timeout ?? DefaultTimeout;
    }

    public MusicServiceType MusicServiceType => MusicServiceType.LastFm;

    public async Task<MusicArtistBase> GetArtistByMBID(string mbid)
    {
        try
        {
            var lastFmArtist = await Limited(_lastFmApi.GetArtistInfoByMbidAsync(mbid));

            if (lastFmArtist != null)
            {
                return new LastFmMusicArtist(lastFmArtist);
            }
            else
            {
                throw new Exception($"Artist with MBID [{mbid}] was not found on LastFM.");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error while fetching artist by MBID [{mbid}] from LastFM.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"Request to LastFM for MBID [{mbid}] timed out.", ex);
        }
        catch (TimeoutException ex)
        {
            throw new Exception($"LastFM did not answer for MBID [{mbid}] in {_timeout.TotalSeconds} s.", ex);
        }
        catch (ServiceException ex)
        {
            throw new Exception($"Hqub.Lastfm returns error with code [{ex.ErrorCode}] and message [{ex.Message}]");
        }
        catch (Exception ex)
        {
            throw new Exception($"An unexpected error occurred while getting artist by MBID [{mbid}].", ex);
        }
    }

    public AvailableSearchType GetAvailableSearch() => availableSearch;

    public async Task<MusicArtistBase> SearchArtistByName(string name)
    {
        try
        {
            var lastFmArtist = await Limited(_lastFmApi.GetArtistInfoAsync(name));

            if (lastFmArtist != null)
            {
                return new LastFmMusicArtist(lastFmArtist);
            }
            else
            {
                throw new Exception($"Artist with name [{name}] was not found on LastFM.");
            }
        }
        catch (HttpRequestException ex)
        {
            throw new Exception($"Network error while fetching artist by name [{name}] from LastFM.", ex);
        }
        catch (TaskCanceledException ex)
        {
            throw new Exception($"Request to LastFM for name [{name}] timed out.", ex);
        }
        catch (TimeoutException ex)
        {
            throw new Exception($"LastFM did not answer for name [{name}] in {_timeout.TotalSeconds} s.", ex);
        }
        catch (Exception ex)
        {
            throw new Exception($"An unexpected error occurred while getting artist by name [{name}].", ex);
        }
    }

    /// <inheritdoc />
    /// <remarks>Last.fm pages from 1: the offset is a multiple of the limit, as the bot turns whole pages.</remarks>
    public async Task<IReadOnlyList<MusicArtistBase>> SearchArtistsByName(string name, int limit, int offset)
    {
        var artists = await Limited(_lastFmApi.SearchArtistsAsync(name, page: (offset / limit) + 1, limit));
        return artists.Select(x => new LastFmMusicArtist(x)).ToList<MusicArtistBase>();
    }

    private Task<T> Limited<T>(Task<T> call)
    {
        return call.WaitAsync(_timeout, _timeProvider);
    }

    public async Task<List<MusicTrackBase>> SearchTopTracks(MusicArtistBase artist)
    {
        var tracks = await GetTopTracksByMbid(artist.MBID);
        if (tracks.Count == 0 && !string.IsNullOrWhiteSpace(artist.Name))
        {
            tracks = await Limited(_lastFmApi.GetTopTracksAsync(artist.Name, TOP_TRACKS_LIMIT));
        }
        return tracks.Select(x => new LastFmMusicTrack(x)).ToList<MusicTrackBase>();
    }

    private async Task<IReadOnlyList<Track>> GetTopTracksByMbid(string? mbid)
    {
        if (string.IsNullOrWhiteSpace(mbid))
        {
            return Array.Empty<Track>();
        }
        try
        {
            return await Limited(_lastFmApi.GetTopTracksByMbidAsync(mbid, TOP_TRACKS_LIMIT));
        }
        catch (ServiceException)
        {
            // Last.fm does not know every MusicBrainz id: the artist name is tried next.
            return Array.Empty<Track>();
        }
    }

    public Task<MusicTrackBase?> SearchTrack(string artistName, string trackName)
    {
        throw new NotImplementedException();
    }
}
