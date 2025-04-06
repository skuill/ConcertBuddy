namespace MusicSearcher.Model
{
    public class MusicSetlists
    {
        public List<MusicSetlist> Setlist { get; set; }

        /// <summary>
        /// Gets or sets the total amount of items matching the query
        /// </summary>
        public int Total { get; set; }

        /// <summary>
        /// Gets or sets current page
        /// </summary>
        public int Page { get; set; }

        /// <summary>
        /// Gets or sets the amount of items you get per page
        /// </summary>
        public int ItemsPerPage { get; set; }

        /// <summary>
        /// Gets the total amount of pages returned by API
        /// </summary>
        public int TotalPages
        {
            get
            {
                if (ItemsPerPage == 0)
                {
                    return 0;
                }

                if (ItemsPerPage > Total)
                {
                    return 1;
                }

                return (int)Math.Floor((double)Total / (double)ItemsPerPage);
            }
        }

        public override string ToString()
        {
            return string.Format("Count = {0}", Setlist == null ? 0 : Setlist.Count);
        }
    }
}
