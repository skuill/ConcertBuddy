using System.Collections.Generic;

namespace Yandex.Music.Api.Models.Search
{
    public class YSearchSuggest
    {
        #region ��������

        public YSearchBest Best { get; set; }
        public List<string> Suggestions { get; set; }

        #endregion
    }
}