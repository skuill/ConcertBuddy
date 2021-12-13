using System;
using System.Collections.Generic;
using System.Linq;
using Genius;
using SetlistFmAPI;
using SetlistFmAPI.Models;
using LyricsScraper;
using LyricsScraper.AZLyrics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SetlistFmAPI.Http;
using LyricsScraper.Abstract;
using LyricsScraper.Common;

namespace ConcertBuddy.ConsoleApp
{
    public class Program
    {
        // Spotify
        // Client ID c75856d9e7454368aca6fe620a103d29


        public static void Main(string[] args)
        {
            var serviceCollection = new ServiceCollection();
            ConfigureServices(serviceCollection);

            var serviceProvider = serviceCollection.BuildServiceProvider();
            var logger = serviceProvider.GetService<ILogger<Program>>();

            string artistName = "Parkway Drive";
            logger.LogInformation(artistName);
            /// Get setlist for artist

            // sativkv@gmail.com API
            string setlistApiKey = AppSettings.SetlistFmApiKey;

            ISetlistFmClient setlistFmClient = serviceProvider.GetService<ISetlistFmClient>();
            setlistFmClient.WithApiKey(setlistApiKey);

            var artists = setlistFmClient.SearchArtists(artistName).GetAwaiter().GetResult();
            var artist = artists.Items.FirstOrDefault();

            var setlists = setlistFmClient.SearchArtistSetlists(artist.MBID).GetAwaiter().GetResult();
            logger.LogInformation($"setlists: {setlists.Items.Count}");

            /// Get lyric for first song in setlist
            var songName = setlists.Items.FirstOrDefault()
                .Sets.Items.FirstOrDefault()
                .Songs.FirstOrDefault().Name;
            logger.LogInformation($"Song: {songName}");

            var artistAndSong = $"{artistName} {songName}";

            //var geniusClient = new GeniusClient(AppSettings.GeniusClientAccessToken);
            //var searchGeniusResult = geniusClient.SearchClient.Search(artistAndSong).GetAwaiter().GetResult();
            //if (searchGeniusResult.Meta.Status != 200)
            //    Console.WriteLine($"ERROR search genius. Code: {searchGeniusResult.Meta.Status}. Message: {searchGeniusResult.Meta.Message}");
            //var artistAndSongHit = searchGeniusResult.Response.Hits.First(x => string.Equals(x.Result.PrimaryArtist.Name, artistName, StringComparison.OrdinalIgnoreCase));
            //Console.WriteLine($"Genius artist and song url: {artistAndSongHit.Result.Url}");

            //// https://genius.com/Parkway-drive-wishing-wells-lyrics
            //var lyricUrl = artistAndSongHit.Result.Url;
            //var lyricUrl = "https://genius.com/Parkway-drive-wishing-wells-lyrics";
            //var songLyrics = GeniusHtmlParser.GetLyric(lyricUrl).GetAwaiter().GetResult();
            //Console.WriteLine(songLyrics);

            ILyricsScraperUtil lyricsScraperUtil = serviceProvider.GetService<ILyricsScraperUtil>();
            ILyricGetter lyricGetter = serviceProvider.GetService<ILyricGetter>();
            lyricsScraperUtil.AddGetter(lyricGetter);
            var lyric = lyricsScraperUtil.SearchLyric(artistName, songName);
            logger.LogInformation(lyric);
        }

        private static void ConfigureServices(IServiceCollection services)
        {
            services.AddLogging(configure => configure.AddConsole())
                    .Configure<LoggerFilterOptions>(options => options.MinLevel = LogLevel.Debug)
                    .AddScoped<ISetlistFmClient, SetlistFmClient>()
                    .AddScoped<IHttpClient, HttpSetlistWebClient>()
                    .AddScoped<IParser, AZLyricsParser>()
                    .AddScoped<IWebClient, HtmlAgilityWebClient>()
                    .AddScoped<ILyricGetter, AZLyricsGetter>()
                    .AddScoped<ILyricsScraperUtil, LyricsScraperUtil>();
        }
    }
}