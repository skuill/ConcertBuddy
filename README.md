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
* [SetlistNet](https://github.com/MolinRE/SetlistNet) - C# library to interact with setlist.fm API.

## Documentation

#### Hosting
The bot is hosted on a [DigitalOcean droplet](https://www.digitalocean.com/products/droplets) and runs in a Docker container from the [Docker Hub repository](https://hub.docker.com/repository/docker/skuill/concertbuddyconsoleapp).

#### Deployment
1. A release builds the image from the [Dockerfile](src/ConcertBuddy.ConsoleApp/Dockerfile), pushes it to Docker Hub and restarts the bot on the server ([cicd.yml](.github/workflows/cicd.yml)); [deploy_manual_droplet.yml](.github/workflows/deploy_manual_droplet.yml) does the restart by hand.
2. The server's compose file and settings (with the keys) are kept in a separate server configuration repository and deployed from there; nothing is edited on the server by hand.
3. Locally: create `appsettings.json` from [appsettings.template.json](src/ConcertBuddy.ConsoleApp/appsettings.template.json) (settings: [Configuration.cs](src/ConcertBuddy.ConsoleApp/Configuration.cs)) and run the image with it: `docker run --rm -it -v "$PWD/appsettings.json:/app/appsettings.json" skuill/concertbuddyconsoleapp:latest`.

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
If you want to support this project or my work in general, you can donate via the links below. 

This is always optional! Thank you! 😉

 * [!["Buy Me A Coffee"](https://www.buymeacoffee.com/assets/img/custom_images/orange_img.png)](https://www.buymeacoffee.com/skuill)
 * [!["PayPal Donate Button"](https://www.paypalobjects.com/en_US/i/btn/btn_donateCC_LG.gif)](https://paypal.me/VitalyKvashin)
 * Crypto. Send only on the networks listed, a coin sent on another network may be lost:

| Network | Coins | Address |
|---|---|---|
| Bitcoin | BTC | `bc1q02d7p4qhjhjn5mnu99wdfc2j06d364jt8qeq0c` |
| Ethereum, Base, Arbitrum, Polygon, BNB Chain | ETH, USDT, USDC | `0xa56541B7DF6263ab84c3e1B69bD37f2a6d848412` |
| Solana | SOL, USDT, USDC | `BZcuV721p9XPsdreiKY9rsmxZDVf8qHNRk3EoPp25nrS` |
| Tron | USDT (TRC-20) | `TGyoTufYzVPU6igcQmFGwH1SFrNes9q3UP` |
