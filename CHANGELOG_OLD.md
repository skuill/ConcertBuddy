Old releases are collected here. For new ones, see the [releases page](https://github.com/skuill/ConcertBuddy/releases).

## Release Notes
#### v1.1.0 (01.01.25):
* Use Hqub.Last.fm instead of Inflatable.Lastfm.
* Updated external libraries to the latest version.
* Refactoring. Added data validation in commands. In the model, properties are now nullable.

#### v1.0.0 (15.09.24):
* Migration from .NET 6 to .NET 8
* Use FakeItEasy instead of Moq
* Optimize docker image size by using alpine in dockerfile
* Updated external libraries to the latest version

#### v0.1.1 (03.09.23):
* Fixed a problem with lyric searching ([Issue 33](https://github.com/skuill/ConcertBuddy/issues/33))
* Use LyricsScraperNET library instead of LyricsScraper project.
* Updated external libraries to the latest version (Spotify, Telegram, Yandex)
  
#### v0.1.0 (28.10.22):
BreakingChanges:
 * Refactoring. MusicSearcherClient has many responsibilities and bottleneck ([Issue 30](https://github.com/skuill/ConcertBuddy/issues/30))
 * Use nuget package of Yandex api instead of project reference ([Issue 21](https://github.com/skuill/ConcertBuddy/issues/21))

Bugfixes:
 * Map properly MusicBrainz to Spotify track while searching ([Issue 29](https://github.com/skuill/ConcertBuddy/issues/29))
 * Track searching will return nothing if track not found in spotify ([Issue 27](https://github.com/skuill/ConcertBuddy/issues/27))
 * Can't find Wu tang artist's information and tracks ([Issue 28](https://github.com/skuill/ConcertBuddy/issues/28))
 * Can't initialize Yandex client! System.Net.WebException: The remote server returned an error: (400) Bad Request. ([Issue 31](https://github.com/skuill/ConcertBuddy/issues/31))
#### v0.0.8 (04.02.22. It's my Bday 🎂):
Bugfixes:
 * Track searching will return nothing if track not found in spotify ([Issue 27](https://github.com/skuill/ConcertBuddy/issues/27))
 * Disable page navigation if nothing found ([Issue 26](https://github.com/skuill/ConcertBuddy/issues/26))
#### v0.0.7 (30.01.22):
 * Added receiving artist's top 10 tracks from Spotify ([Issue 9](https://github.com/skuill/ConcertBuddy/issues/9))
#### v0.0.6 (25.01.22):
 * Catch unhandled exceptions and log them. ([Issue 24](https://github.com/skuill/ConcertBuddy/issues/24))
 * Configure write logs to file with rotation with docker volume. ([Issue 25](https://github.com/skuill/ConcertBuddy/issues/25))
#### v0.0.5 (24.01.22):
 * Listening to the track directly in the telegram. Use Yandex track storage. ([Issue 8](https://github.com/skuill/ConcertBuddy/issues/8))
#### v0.0.4 (21.01.22):
Bugfixes:
 * Made SearchLyric async. ([Issue 16](https://github.com/skuill/ConcertBuddy/issues/16))
 * Remove async from telegram messages validation. Move callback messages in MessageHelper. ([Issue 18](https://github.com/skuill/ConcertBuddy/issues/18))
#### v0.0.3 (21.01.22):
 * Migrate app.config to appsettings.json with DI.
#### v0.0.2 (19.01.22):
 * Fixed async task order in SearchArtistByMBID method. 
 * Rename methods with Async. 
 * Added some TODO comments.
#### v0.0.1 (12.01.22):
 * Added MemoryCache when searching for artists by mbid to reduce API calls. 
 * Fixed logs format with date and scope. 
 * Publish docker version 0.0.1 and deploy on server. 🎉
