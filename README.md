# ConcertBuddy

<img src="https://github.com/skuill/ConcertBuddy/blob/master/resources/BotImage.jpg" width="30%" height="30%">

## Table of contents
- [General info](#general-info)
- [Features](#features)
- [Feedback](#feedback)
- [Technologies](#technologies)
- [Built with](#built-with)
- [Documentation](#documentation)
- [TODO](#todo)
- [Release Notes](#release-notes)
- [Support](#support)

## General info
Are you planning to go to the concert? 
This telegram bot will be your companion, like a good buddy! 😎 

He can tell you the biography of the artist/band, find out the actual setlist from the last concerts, play the track and show the lyrics.

Just write the artist/band name to the **[ConcertBuddy](https://t.me/concert_buddy_bot)** bot and enjoy!

## Features

A few of the things you can do with ConcertBuddy:
* ✅ Search artist / band by name
  * `MusicBrainz`
* ✅ Read artist / band biography
  * `Last.fm`
* ✅ View setlists from recent concerts
  * `Setlist.fm`
* ✅ Listen to tracks
  * Directly from: `Yandex`
  * External link: `Spotify`
* ✅ Read the lyrics of the tracks
  * `AZLyrics`

## Feedback

Feel free to send me feedback on [Telegram](https://t.me/skuill) or [file an issue](https://github.com/skuill/ConcertBuddy/issues/new). Feature requests are always welcome.

## Technologies
Project is created with:
* `.NET: 6.0`
* `Microsoft Visual Studio Community 2022`
* `docker`

## Built with
* [Telegram.Bot](https://github.com/TelegramBots/telegram.bot) -  the most popular .NET Client for 🤖 Telegram Bot API. 
* [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - a client for Spotify's Web API, written in .NET 
* [MusicBrainzAPI](https://github.com/avatar29A/MusicBrainz) - Implementation of the MuzicBrainz API v2 
* [Inflatable.Lastfm](https://github.com/inflatablefriends/lastfm) - Last.fm SDK for modern .NET platforms 
* [Genius.NET](https://github.com/prajjwaldimri/Genius.NET) - C# library to access the Genius REST API in .NET 
* [HtmlAgilityPack](https://html-agility-pack.net/) - agile HTML parser that builds a read/write DOM and supports plain XPATH or XSLT (you actually don't HAVE to understand XPATH nor XSLT to use it, don't worry...)
* [Serilog](https://serilog.net/) - simple .NET logging with fully-structured events
* [Yandex.Music API](https://github.com/K1llMan/Yandex.Music.Api) - unofficial wrapper for the Yandex.Music API

## Documentation

#### Hosting
The bot is launched on the Ubuntu 20.04 64bit server [VDS Selectel](https://vds.selectel.ru/) in a docker container from [docker hub repository](https://hub.docker.com/repository/docker/skuill/concertbuddyconsoleapp).

#### Deployment
1. Create and configure `ConcertBuddy/ConcertBuddy.ConsoleApp/appsettings.json` file with settings [Configuration.cs](ConcertBuddy.ConsoleApp/Configuration.cs) from template [appsettings.template.json](ConcertBuddy.ConsoleApp/appsettings.template.json). 
2. Build and publish docker image with [Dockerfile](ConcertBuddy.ConsoleApp/Dockerfile) to [docker hub registry](https://hub.docker.com/). 
3. Use [docker-compose.yml](docker-compose.yml) to run container.

#### Environments
| Name            | Link               |
| --------------- |:------------------:|
| Development     | **[TestConcertBuddy](https://t.me/test_concert_buddy_bot)** |
| Production      | **[ConcertBuddy](https://t.me/concert_buddy_bot)** |

#### Logging
The serilog adapter is used for event logging. Logs are written to the console, debug, file. Log rotation is configured. Logging settings are set in appsettings.json. Example: [appsettings.template.json](ConcertBuddy.ConsoleApp/appsettings.template.json)

## TODO
Visit [github project page](https://github.com/users/skuill/projects/1) or [issue board](https://github.com/skuill/ConcertBuddy/issues)

## Release Notes
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

## Support
If you want to support this project or my work in general, you can donate via the link below. 

This will always be optional! Thank you! 😉

[![Tinkoff Donate Button](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.tinkoff.ru/cf/3MNYeRds3s)
