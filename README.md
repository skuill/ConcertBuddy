# ConcertBuddy

## Table of contents
- [General info](#general-info)
- [Features](#features)
- [Feedback](#feedback)
- [Technologies](#technologies)
- [Built with](#built-with)
- [Hosting](#hosting)
- [TODO](#to-do)

## General info
Are you planning to go to the concert? 
This telegram bot will be your companion, like a good buddy! 😎 

He can tell you the biography of the artist/band, find out the actual setlist from the last concerts, play the track and show the lyrics.

Just write the artist/band name to the **[ConcertBuddy](https://t.me/concert_buddy_bot)** bot and enjoy!

## Features

A few of the things you can do with ConcertBuddy:
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

## Hosting
The bot is launched on the Ubuntu 20.04 64bit server [VDS Selectel](https://vds.selectel.ru/) in a docker container.

## TODO
* Add scraping lyrics from sites: 
  * [Genius](https://www.genius.com)
  * [Musixmatch](https://www.musixmatch.com/) 
* Add integration with other audio services to receive tracks: 
  * [YandexMusic](https://music.yandex.ru/home)
  * [itunes](https://www.apple.com/ru/itunes/)
* Add receiving top 5 tracks from spotify
* Configure docker restart policy on server
* Add temporary cache for requested artists to reduce the number of api calls
* Configure pipeline to continuously build and deliver docker images to the server
* Configure system observability (opentelemetry): traces, logs and metrics
* Set up a release policy, versioning
