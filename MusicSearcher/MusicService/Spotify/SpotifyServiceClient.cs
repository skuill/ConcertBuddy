﻿using MusicSearcher.Converter;
using MusicSearcher.Model;
using MusicSearcher.MusicService.Abstract;
using SpotifyAPI.Web;

namespace MusicSearcher.MusicService.Spotify
{
    internal class SpotifyServiceClient : IMusicServiceClient
    {
        private SpotifyClient _spotifyClient;
        private AvailableSearchType availableSearch = AvailableSearchType.Name;

        public SpotifyServiceClient(string cliendID, string clientSecret)
        {
            try
            {
                var config = SpotifyClientConfig
                       .CreateDefault()
                       .WithRetryHandler(new SimpleRetryHandler() { RetryAfter = TimeSpan.FromSeconds(1) })
                       .WithAuthenticator(new ClientCredentialsAuthenticator(cliendID, clientSecret));

                _spotifyClient = new SpotifyClient(config);
            }
            catch (Exception)
            {
                throw;
            }
        }

        public Task GetArtistByMBID(MusicArtist artist, string mbid)
        {
            throw new NotImplementedException();
        }

        public async Task<List<MusicTrack>> SearchTopTracks(MusicArtist artist)
        {
            if (artist != null && artist.SpotifyArtist != null)
            {
                // Country code should be in ISO 3166-1
                string formattedCountry = RegionConverter.ConvertToTwoLetterISO(artist.Country);
                var spotifyTracks = await GetSpotifyTopTracks(artist.SpotifyArtist, formattedCountry);
                if (spotifyTracks != null)
                {
                    return spotifyTracks.Select(t => new MusicTrack() { SpotifyTrack = t }).ToList();
                }
            }
            return null;
        }

        private async Task<IEnumerable<FullTrack>> GetSpotifyTopTracks(FullArtist artist, string country)
        {
            var topTracks = await _spotifyClient.Artists.GetTopTracks(artist.Id, new ArtistsTopTracksRequest(country));
            if (topTracks != null && topTracks.Tracks != null && topTracks.Tracks.Any())
            {
                return topTracks.Tracks;
            }
            else
            {
                throw new Exception($"Can't get top tracks from Spotify for artist [{artist.Name}] with id [{artist.Id}]");
            }
        }

        public async Task SearchTrack(MusicTrack track, string artistName, string trackName)
        {
            var searchTrack = await _spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Track, $"{artistName} - {trackName}"));
            if (searchTrack == null
                || searchTrack.Tracks == null
                || searchTrack.Tracks.Total == 0)
            {
                throw new Exception($"Can't get track [{trackName}] for artist [{artistName}] from Spotify.");
            }
            // We can't compare artistName. For example for artist "ноганно" actual spotify name is "noganno".
            track.SpotifyTrack = searchTrack.Tracks.Items.First(t => t.Artists != null);
        }

        public AvailableSearchType GetAvailableSearch() => availableSearch;

        public async Task SearchArtistByName(MusicArtist artist, string name)
        {
            var searchArtist = await _spotifyClient.Search.Item(new SearchRequest(SearchRequest.Types.Artist, name));
            if (searchArtist == null
                || searchArtist.Artists == null
                || searchArtist.Artists.Total == 0)
            {
                throw new Exception($"Can't get artist [{name}] from Spotify.");
            }
            // We can't compare artistName. For example for artist "ноганно" actual spotify name is "noganno".
            artist.SpotifyArtist = searchArtist.Artists.Items.First();
        }
    }
}