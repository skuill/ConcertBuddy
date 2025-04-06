namespace MusicSearcher.Model
{
    public class MusicSong
    {
        public string? Name { get; set; }

        public string? CoverName { get; set; }

        public string? WithName { get; set; }

        public string? Info { get; set; }

        public override string ToString()
        {
            string result = $"{Name}";
            if (CoverName != null)
                result += $" ({CoverName} cover)";
            if (WithName != null)
                result += $" (with {WithName})";
            if (Info != null)
                result += $" ({Info})";
            return result;
        }
    }
}
