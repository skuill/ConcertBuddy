using SpotifyAPI.Web;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Yandex.Music.Api.Models.Track;
using Yandex.Music.Client.Extensions;

namespace MusicSearcher.Model
{
    public class MusicTrack
    {
        public FullTrack SpotifyTrack { get; set; }

        public YTrack YandexTrack { get; set; }

        public string Name => SpotifyTrack?.Name;

        public string DownloadLink => YandexTrack?.GetLink();
    }
}
