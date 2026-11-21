# External services

How the bot calls the music services. Settings: `src/MusicSearcher/Http/`.

| Service | Limit of the service | What the bot does |
|---|---|---|
| MusicBrainz | about 1 request per second, `503` over the limit, needs an application User-Agent | 1 request per second, own User-Agent, retries |
| Setlist.fm | our key: 2 requests per second and 1,440 per day | 2 requests per second, retries, a `404` means "no data" |
| Last.fm, Yandex Music, lyrics sites | | the client libraries do not accept our `HttpClient`, no retries here |

## Retries

Retries come after `408`, `429`, `5xx`, network errors and a timeout of 15 seconds per attempt:

- **MusicBrainz:** up to 5 retries, 1 second apart (6 attempts in about 6 seconds).
- **Setlist.fm:** up to 3 retries after 2, 4 and 8 seconds.

The delays are always ours: a busy MusicBrainz sends `Retry-After: 0` or `1`, and following it made all retries come at once and fail too.

### A busy MusicBrainz

When MusicBrainz is overloaded (often in the European evening), it sheds requests of all clients, not ours for our rate: `503` with `X-RateLimit-Zone: global` and `X-RateLimit-Who: global`, or `X-RateLimit-Zone: search` and `X-RateLimit-Who: search-shed`, always with `Retry-After: 1`. Measured on 2026-11-21 (#93): 35 of 60 searches 1.1 seconds apart got `503`, mixed with successes and at most 5 in a row; a new connection or a pause before the request made no difference. Each request is shed on its own, so many attempts one second apart work better than a few with long delays: four attempts all failed with a chance of about 11%, six of about 4%. When all of them fail, the user gets "busy, try again" (Last.fm as a fallback: #91).

`MusicBrainzBusyLogger` logs every busy answer with these headers and the time since the previous request (`docs/logging.md`).

Requests over the limit wait in a queue of up to 20 requests; when the queue is full, the request fails.

## Errors

When MusicBrainz does not answer after the retries, `MusicSearcherClient` throws `MusicServiceUnavailableException` for the artist search and the artist by MBID, and the bot shows "busy, try again" instead of "nothing found". A `400` or `404` for an MBID means "no such artist" (an empty artist).

SetlistNet does not check the status code, so `EnsureSuccessHandler` turns an error answer into an exception. `SetlistFmServiceClient` returns no data for a `404` (setlist.fm has nothing, logged at the Information level) and throws `MusicServiceUnavailableException` for anything else (a `Warning`), so the bot shows "busy, try again".
