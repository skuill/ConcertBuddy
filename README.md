# ConcertBuddy

<img src="https://github.com/skuill/ConcertBuddy/blob/master/resources/BotImage.jpg" width="30%" height="30%">

## Table of contents
- [General info](#general-info)
- [Features](#features)
- [Feedback](#feedback)
- [Technologies](#technologies)
- [Built with](#built-with)
- [Documentation](#documentation)
- [TODO](#to-do)
- [Release Notes](#release-notes)

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
  * `Spotify`
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

## Documentation

#### Hosting
The bot is launched on the Ubuntu 20.04 64bit server [VDS Selectel](https://vds.selectel.ru/) in a docker container.

#### Deployment
1. Create and configure `ConcertBuddy/ConcertBuddy.ConsoleApp/app.config` file with settings from [Configuration.cs](ConcertBuddy.ConsoleApp/Configuration.cs).
2. Build and publish docker image with [Dockerfile](ConcertBuddy.ConsoleApp/Dockerfile).
3. Use [docker-compose.yml](docker-compose.yml) to run container.

#### Environments
| Name            | Link               |
| --------------- |:------------------:|
| Development     | **[TestConcertBuddy](https://t.me/test_concert_buddy_bot)** |
| Production      | **[ConcertBuddy](https://t.me/concert_buddy_bot)** |

## TODO
* [ ] Add scraping lyrics from sites: 
  * [ ] [Genius](https://www.genius.com)
  * [ ] [Musixmatch](https://www.musixmatch.com/) 
* [ ] Add integration with other audio services to receive tracks: 
  * [ ] [YandexMusic](https://music.yandex.ru/home)
  * [ ] [itunes](https://www.apple.com/ru/itunes/)
* [ ] Listening to the track directly in the telegram
* [ ] Add receiving top 5 tracks from spotify
* [x] Configure docker restart policy on server (**11.01.22. Configured restart docker service and containers always**)
* [x] Add temporary cache for requested artists to reduce the number of api calls (**12.01.22. Release v0.0.1**)
* [ ] CI/CD (Configure pipeline to continuously build and deliver docker images to the server)
* [ ] Configure system observability (opentelemetry): traces, logs and metrics
* [ ] Set up a release policy, versioning
* [ ] Creating a setlist for a playlist in a music service

## Release Notes
#### v0.0.1 (12.01.22):
 * Added MemoryCache when searching for artists by mbid to reduce API calls. 
 * Fixed logs format with date and scope. 
 * Publish docker version 0.0.1 and deploy on server. 🎉
