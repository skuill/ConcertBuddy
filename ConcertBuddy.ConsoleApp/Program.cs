using System;
using System.Collections.Generic;
using System.Linq;
using Genius;
using SetlistFmAPI;
using SetlistFmAPI.Models;
using LyricsScraper;
using LyricsScraper.AZLyrics;

namespace ConcertBuddy.ConsoleApp
{
    public class Program
    {
        // Spotify
        // Client ID c75856d9e7454368aca6fe620a103d29


        public static void Main(string[] args)
        {
            string artistName = "Parkway Drive";
            Console.WriteLine(artistName);
            /// Get setlist for artist

            // sativkv@gmail.com API
            string setlistApiKey = AppSettings.SetlistFmApiKey;

            ISetlistFmClient setlistFmClient = new SetlistFmClient(setlistApiKey);

            var artists = setlistFmClient.SearchArtists(artistName).GetAwaiter().GetResult();
            var artist = artists.Items.FirstOrDefault();

            var setlists = setlistFmClient.SearchArtistSetlists(artist.MBID).GetAwaiter().GetResult();
            Console.WriteLine($"setlists: {setlists.Items.Count}");

            /// Get lyric for first song in setlist
            var songName = setlists.Items.FirstOrDefault().Sets.Items.FirstOrDefault().Songs.FirstOrDefault().Name;
            Console.WriteLine($"Song: {songName}");

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

            LyricsScraperUtil.WithGetter(new AZLyricsGetter());
            var lyric = LyricsScraperUtil.SearchLyric(artistName, songName);
            Console.WriteLine(lyric);
        }
    }
}