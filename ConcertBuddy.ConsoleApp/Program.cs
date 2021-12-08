using System;
using System.Collections.Generic;
using System.Linq;
using SetlistFmAPI;
using SetlistFmAPI.Models;

namespace ConcertBuddy.ConsoleApp
{
    public class Program
    {
        // Spotify
        // Client ID c75856d9e7454368aca6fe620a103d29


        public static void Main(string[] args)
        {
            string bandName = "Parkway Drive";

            // sativkv@gmail.com API
            string setlistApiKey = AppSettings.SetlistFmApiKey;

            ISetlistFmClient setlistFmClient = new SetlistFmClient(setlistApiKey);
            
            var artists = setlistFmClient.SearchArtists(bandName).GetAwaiter().GetResult();
            var artist = artists.Items.FirstOrDefault();

            var setlists = setlistFmClient.SearchArtistSetlists(artist.MBID).GetAwaiter().GetResult();
            Console.WriteLine($"setlists: {setlists.Items.Count}");

        }
    }
}