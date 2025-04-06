namespace MusicSearcher.Model
{
    public class MusicSet
    {
        public string? Name { get; set; }

        public int? Encore { get; set; }

        public List<MusicSong> Songs { get; set; }

        public override string ToString()
        {
            string result = "";
            if (Encore != null)
                result = "[Encore " + Encore + "] ";
            else
                result = "Set ";

            return $"{result}{Name}. Songs = {Songs.Count}";
        }
    }
}
