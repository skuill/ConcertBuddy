namespace MusicSearcher.Http;

/// <summary>
/// SetlistNet does not check the status code and turns an error body into an empty object: this handler makes such a response an exception.
/// </summary>
internal sealed class EnsureSuccessHandler : DelegatingHandler
{
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var response = await base.SendAsync(request, cancellationToken);
        if (!response.IsSuccessStatusCode)
        {
            var statusCode = response.StatusCode;
            response.Dispose();
            throw new HttpRequestException($"The service answered {(int)statusCode}.", null, statusCode);
        }
        return response;
    }
}
