# External services

How the bot calls the music services. Settings: `src/MusicSearcher/Http/` (`MusicServiceHttpOptions`).

| Service | Limit of the service | What the bot does |
|---|---|---|
| MusicBrainz | about 1 request per second, `503` over the limit, needs an application User-Agent | 1 request per second, own User-Agent, 3 attempts in up to 4 seconds per turn, answers cached in memory for a day |
| Setlist.fm | our key: 2 requests per second and 1,440 per day | 2 requests per second, retries, 10 seconds at most, a `404` means "no data" |
| Lyrics sites (LyricsScraperNET) | | 10 seconds per provider (`ProviderTimeout` of the library) |
| Last.fm | | the client library takes no `HttpClient`: no retries, the bot stops waiting after 4 seconds; the second turn of the artist search and card |
| Yandex Music | | the client library takes no `HttpClient`: no retries, no timeout of ours |

## Retries and timeouts

Retries come after `408`, `429`, `5xx`, network errors and a timed out attempt. Every call has two timeouts: one attempt, and the whole call with its retries and the time in the queue of the rate limiter (#91).

| Service | Retries | One attempt | The whole call |
|---|---|---|---|
| MusicBrainz | up to 2, 1 second apart | 3 seconds | 4 seconds |
| Setlist.fm | up to 2, after 2 and 4 seconds | 5 seconds | 10 seconds |

How the numbers were chosen: MusicBrainz answers in 0.05 to 0.6 seconds, about 1.4 on a new connection (TLS), a `503` in 0.05 to 0.6 seconds (measured 2026-11-21); Setlist.fm took 0.8 seconds for the setlists screen on the dev bot. An attempt gets two to three times the slowest answer. The whole call is what a user still waits for in a chat; the default of `AddStandardResilienceHandler` (10 seconds per attempt, 30 in total) is meant for calls between services and is too long here. No hedging: a second request in parallel breaks the 1 request per second rule.

The delays are always ours: a busy MusicBrainz sends `Retry-After: 0` or `1`, and following it made all retries come at once and fail too.

### A busy MusicBrainz

When MusicBrainz is overloaded (often in the European evening), it sheds requests of all clients, not ours for our rate: `503` with `X-RateLimit-Zone: global` and `X-RateLimit-Who: global`, or `X-RateLimit-Zone: search` and `X-RateLimit-Who: search-shed`, always with `Retry-After: 1`. Measured on 2026-11-21 (#93): 35 of 60 searches 1.1 seconds apart got `503`, mixed with successes and at most 5 in a row; a new connection or a pause before the request made no difference. Each request is shed on its own, so attempts one second apart work better than a few with long delays: three attempts all fail with a chance of about 20% at that rate, and then Last.fm answers (below).

`MusicBrainzBusyLogger` logs every busy answer with these headers and the time since the previous request (`docs/logging.md`).

Requests over the limit wait in a queue of up to 20 requests; when the queue is full, the request fails.

## Cache

The MusicBrainz client asks `MemoryRequestCache` before every request, by the request URL: the artist search, the artist by MBID and the recording search of a track come from memory for a day, within 16 MB. An error answer is not cached. On top of it `MusicSearcherClient` keeps the full artist (with what the other services know) for 10 minutes. Both live as long as the bot runs; a restart starts empty.

## In turn: MusicBrainz, Last.fm, MusicBrainz

The artist search and the card ask the services in turn (#91): MusicBrainz (one call, up to 3 attempts in about 2 seconds, 4 at most), then Last.fm (up to 4 seconds), then MusicBrainz once more, and only then "busy, try again". Nothing is remembered between the searches: every search starts with MusicBrainz, which has the country, type and years. When MusicBrainz is down, the user gets Last.fm in about 2 to 3 seconds; the longest wait, when all three turns hang, is about 12 seconds.

No circuit breaker: a breaker opens on a share of failures within a sampling window and a minimum number of requests (the standard handler: 10% of at least 100 requests in 30 seconds). The bot makes a few requests an hour, so such a breaker would never open, and one that opens after a single failure would leave a working MusicBrainz unused.

What Last.fm gives:

- **Search:** `artist.search`, the same page (Last.fm pages from 1). Only artists with an MBID are kept, because the card, the setlists and the buttons go by MBID. The list has names and listeners (Last.fm has no country, type or years).
- **Card:** `artist.getInfo` by the MBID: name, photo, biography, listeners, the Last.fm link. Such a card is not cached, so the next tap shows the full card again once MusicBrainz answers.
- Both screens say in italics that MusicBrainz is busy and the data comes from Last.fm.
- When Last.fm is not configured, does not answer or has no artist with an MBID for the search, MusicBrainz gets its second turn.

The other screens that need the artist (setlists, upcoming, top tracks, biography) get the same Last.fm card, so they work while MusicBrainz is busy. Playing a track still needs MusicBrainz for the recording search.

## Errors

When neither MusicBrainz in both turns nor Last.fm answers, `MusicSearcherClient` throws `MusicServiceUnavailableException` for the artist search and the artist by MBID, and the bot shows "busy, try again" instead of "nothing found". A `400` or `404` for an MBID means "no such artist" (an empty artist), and Last.fm is not asked then.

SetlistNet does not check the status code, so `EnsureSuccessHandler` turns an error answer into an exception. `SetlistFmServiceClient` returns no data for a `404` (setlist.fm has nothing, logged at the Information level) and throws `MusicServiceUnavailableException` for anything else (a `Warning`), so the bot shows "busy, try again".
