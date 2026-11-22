using Hqub.Lastfm.Entities;

namespace MusicSearcher.MusicService.LastFm;

/// <summary>
/// The Last.fm calls used by <see cref="LastFmServiceClient"/>.
/// </summary>
public interface ILastFmApi
{
    Task<Artist> GetArtistInfoByMbidAsync(string mbid);

    Task<Artist> GetArtistInfoAsync(string name);

    /// <summary>
    /// One page of <c>artist.search</c>, pages from 1.
    /// </summary>
    Task<IReadOnlyList<Artist>> SearchArtistsAsync(string name, int page, int limit);

    Task<IReadOnlyList<Track>> GetTopTracksByMbidAsync(string mbid, int limit);

    Task<IReadOnlyList<Track>> GetTopTracksAsync(string artistName, int limit);
}
