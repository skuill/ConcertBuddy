using Newtonsoft.Json;

namespace Yandex.Music.Api.Models.Artist
{
    public class YMetroStation
    {
        #region ��������

        [JsonProperty("line-color")]
        public string LineColor { get; set; }
        public string Title { get; set; }

        #endregion
    }
}