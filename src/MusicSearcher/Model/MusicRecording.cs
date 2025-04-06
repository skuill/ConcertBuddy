namespace MusicSearcher.Model
{
    /// <summary>
    /// A recording is an entity which can be linked to tracks on releases. 
    /// Each track must always be associated with a single recording, 
    /// but a recording can be linked to any number of tracks.
    /// </summary>
    public class MusicRecording
    {
        /// <summary>
        /// Gets or sets the MusicBrainz id.
        /// </summary>
        public string? Id { get; internal set; }

        public string? Title { get; internal set; }
    }
}
