namespace Yandex.Music.Api.Models.Playlist
{
    public class YPlaylistUidPair
    {
        #region ����

        public override string ToString()
        {
            return $"{Uid}:{Kind}";
        }

        #endregion

        #region ��������

        public string Kind { get; set; }
        public string Uid { get; set; }

        #endregion
    }
}