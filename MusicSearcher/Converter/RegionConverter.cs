using System.Globalization;

namespace MusicSearcher.Converter
{
    public static class RegionConverter
    {
        public const string DEFAULT_REGION_CODE = "US";

        public static string ConvertToTwoLetterISO(string country)
        {
            if (string.IsNullOrEmpty(country))
                return DEFAULT_REGION_CODE;

            try
            {
                RegionInfo region = new RegionInfo(country);
                return region.TwoLetterISORegionName;
            }
            catch (Exception ex)
            {
                return DEFAULT_REGION_CODE;
            }
        }
    }
}
