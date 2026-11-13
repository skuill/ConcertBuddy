# External services

How the bot calls the music services. Settings: `src/MusicSearcher/Http/`.

| Service | Limit of the service | What the bot does |
|---|---|---|
| MusicBrainz | about 1 request per second, `503` over the limit, needs an application User-Agent | 1 request per second, own User-Agent, retries |
| Setlist.fm | our key: 2 requests per second and 1,440 per day | 2 requests per second, retries, a `404` means "no data" |
| Last.fm, Yandex Music, lyrics sites | | the client libraries do not accept our `HttpClient`, no retries here |

## Retries

For MusicBrainz and Setlist.fm: up to 3 retries after 2, 4 and 8 seconds, after `408`, `429`, `5xx`, network errors and a timeout of 15 seconds per attempt. The delays are always ours: a busy MusicBrainz sends `Retry-After: 0` or `1`, and following it made all retries come at once and fail too.

Requests over the limit wait in a queue of up to 20 requests; when the queue is full, the request fails.

## Errors

When MusicBrainz does not answer after the retries, `MusicSearcherClient` throws `MusicServiceUnavailableException` for the artist search and the artist by MBID, and the bot shows "busy, try again" instead of "nothing found". A `400` or `404` for an MBID means "no such artist" (an empty artist).

SetlistNet does not check the status code, so `EnsureSuccessHandler` turns an error answer into an exception. `SetlistFmServiceClient` logs it and returns no data: a `404` at the Information level, anything else as an error.
