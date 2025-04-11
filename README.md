<h1 align="center">ConcertBuddy</h1>
<p align="center">
Your personal Telegram bot for discovering artists, setlists, and lyrics. Perfect for music enthusiasts and concert-goers! 🎵
</p>
<p align="center">
<img src="https://github.com/skuill/ConcertBuddy/blob/master/resources/BotImage.png" align="center" width="40%" height="40%">
</p>
<p align="center">
  <a href="https://github.com/skuill/ConcertBuddy/actions/workflows/cicd.yml" ><img src="https://github.com/skuill/ConcertBuddy/actions/workflows/cicd.yml/badge.svg"/>
  <a href="https://www.codefactor.io/repository/github/skuill/concertbuddy"><img src="https://www.codefactor.io/repository/github/skuill/concertbuddy/badge" alt="CodeFactor" /></a>
  <a href="https://hub.docker.com/r/skuill/concertbuddyconsoleapp">
    <img src="https://img.shields.io/docker/v/skuill/concertbuddyconsoleapp" alt="Docker Image Tag"/>
  </a>
 </a>
</p>
 
## Table of contents
- [General info](#general-info)
- [Features](#features)
- [Feedback](#feedback)
- [Technologies](#technologies)
- [Built with](#built-with)
- [Documentation](#documentation)
- [Roadmap](#roadmap)
- [Release Notes](#release-notes)
- [Support](#support)

## General info
Are you planning to go to a concert?
This Telegram bot will be your companion, like a good buddy! 😎

With ConcertBuddy, you can:
- Explore artist and band biographies.
- Check out setlists from recent concerts.
- Listen to tracks and read their lyrics.

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
  * Different lyric providers: `AZLyrics`, `Genius`, `SongLyrics`, `MusixMatch`, `LyricFind`

## Feedback

For bug reports, suggestions, or feature requests, please use the [issue tracker](https://github.com/skuill/ConcertBuddy/issues/new).

For direct feedback, reach out on [Telegram](https://t.me/skuill).

Feature requests are always welcome.

## Technologies
Project is created with:
* `.NET: 6.0`
* `Microsoft Visual Studio Community 2022`
* `docker`

## Built with
* [Telegram.Bot](https://github.com/TelegramBots/telegram.bot) - the most popular .NET Client for 🤖 Telegram Bot API
* [LyricsScraperNET](https://github.com/skuill/LyricsScraperNET) - 🎼 a library for .NET that provides an API to search for lyrics of a song from the web
* [SpotifyAPI-NET](https://github.com/JohnnyCrazy/SpotifyAPI-NET) - a client for Spotify's Web API, written in .NET 
* [MusicBrainzAPI](https://github.com/avatar29A/MusicBrainz) - Implementation of the MuzicBrainz API v2 
* [Hqub.Last.fm](https://github.com/avatar29A/Last.fm) - Implementation of the Last.fm API for .NET Standard 2.0 or above
* [Genius.NET](https://github.com/prajjwaldimri/Genius.NET) - C# library to access the Genius REST API in .NET 
* [HtmlAgilityPack](https://html-agility-pack.net/) - agile HTML parser that builds a read/write DOM and supports plain XPATH or XSLT (you actually don't HAVE to understand XPATH nor XSLT to use it, don't worry...)
* [Serilog](https://serilog.net/) - simple .NET logging with fully-structured events
* [Yandex.Music API](https://github.com/K1llMan/Yandex.Music.Api) - unofficial wrapper for the Yandex.Music API

## Documentation

#### Hosting
The bot is hosted on an Ubuntu 22.10 64-bit server using a [DigitalOcean droplet](https://www.digitalocean.com/products/droplets) and runs in a Docker container from [docker hub repository](https://hub.docker.com/repository/docker/skuill/concertbuddyconsoleapp). Hosting costs $6/month.

#### Deployment
1. Create and configure `ConcertBuddy/ConcertBuddy.ConsoleApp/appsettings.json` file with settings [Configuration.cs](src/ConcertBuddy.ConsoleApp/Configuration.cs) from template [appsettings.template.json](src/ConcertBuddy.ConsoleApp/appsettings.template.json). 
2. Build and publish docker image usingn the [Dockerfile](src/ConcertBuddy.ConsoleApp/Dockerfile) to [docker hub registry](https://hub.docker.com/). 
3. Use [docker-compose.yml](docker-compose.yml) to run the container.

#### Environments
| Name            | Link               |
| --------------- |:------------------:|
| Development     | **[TestConcertBuddy](https://t.me/test_concert_buddy_bot)** |
| Production      | **[ConcertBuddy](https://t.me/concert_buddy_bot)** |

#### Logging
Event logging is managed by Serilog, with logs written to console, debug, and file. Log rotation is configured. Logging settings can be found in `appsettings.json`. Example: [appsettings.template.json](src/ConcertBuddy.ConsoleApp/appsettings.template.json)

## Roadmap
See the full roadmap on the [Github project page](https://github.com/users/skuill/projects/1) or [Issue board](https://github.com/skuill/ConcertBuddy/issues)

## Release Notes
All new releases with descriptions are available on the [releases page](https://github.com/skuill/ConcertBuddy/releases). 

To view the changes for older releases up to version **1.1.0**, see the file [CHANGELOG_OLD.md](https://github.com/skuill/ConcertBuddy/blob/master/CHANGELOG_OLD.md).

## Support
If you want to support this project or my work in general, you can donate via the link below. 

This is always optional! Thank you! 😉

 * [!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/skuill)
 * [!["Tinkoff Donate Button"](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://www.tinkoff.ru/cf/3MNYeRds3s)
