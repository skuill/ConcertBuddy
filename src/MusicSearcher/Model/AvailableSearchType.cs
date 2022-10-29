namespace MusicSearcher.Model
{
    public enum AvailableSearchType
    {
        None = 0,
        Name = 1,
        MBID = 2,
        All = Name | MBID
    }
}
