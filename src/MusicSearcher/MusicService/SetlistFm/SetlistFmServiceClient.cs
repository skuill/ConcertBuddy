using Microsoft.Extensions.Logging;
using MusicSearcher.Converter;
using MusicSearcher.Model;
using SetlistNet;
using System.Net;

namespace MusicSearcher.MusicService.SetlistFm;

public sealed class SetlistFmServiceClient : ISetlistFmServiceClient
{
    private readonly SetlistApi _setlistApi;
    private readonly ILogger<SetlistFmServiceClient> _logger;

    public SetlistFmServiceClient(ILogger<SetlistFmServiceClient> logger, SetlistApi setlistApi)
    {
        _setlistApi = setlistApi;
        _logger = logger;
    }

    public async Task<MusicSetlists> SearchArtistSetlists(string artistMBID, int page = 1)
    {
        try
        {
            var result = await _setlistApi.ArtistSetlists(artistMBID, page: page);

            if (result == null || result.Setlist == null || result.Setlist.Count == 0)
            {
                _logger?.LogInformation($"Can't find setlists for artist with mbid [{artistMBID}]");
                return null!;
            }
            return result.ToInternal();
        }
        catch (Exception ex)
        {
            LogFailure(ex, $"Can't get setlists for artist with mbid [{artistMBID}]");
            return null!;
        }
    }

    public async Task<MusicSetlist> SearchSetlist(string setlistId)
    {
        try
        {
            var result = await _setlistApi.Setlist(setlistId);
            if (result == null)
            {
                _logger?.LogError($"Can't find setlist by Id: [{setlistId}]");
                return null!;
            }
            return result.ToInternal();
        }
        catch (Exception ex)
        {
            LogFailure(ex, $"Can't get setlist by Id: [{setlistId}]");
            return null!;
        }
    }

    private void LogFailure(Exception ex, string message)
    {
        // Setlist.fm answers 404 when there is no data: it is a normal result, not a failure of the service.
        var level = ex is HttpRequestException { StatusCode: HttpStatusCode.NotFound } ? LogLevel.Information : LogLevel.Error;
        _logger?.Log(level, ex, message);
    }
}
