namespace ConcertBuddy.ConsoleApp.TelegramBot.Callback;

/// <summary>
/// Actions of the buttons, one or two letters to keep the callback data short. Data starting with "/"
/// belongs to the old commands that are not screens yet.
/// </summary>
public static class CallbackActions
{
    /// <summary>
    /// A page of the artist search, also the way back from the card: offset|text. Replaces the list at once.
    /// </summary>
    public const string Search = "s";

    /// <summary>
    /// A new artist search (an example, "Try again"): offset|text. Shows "Searching…" first, the screen before was not a list.
    /// </summary>
    public const string NewSearch = "n";

    /// <summary>
    /// Artist card: mbid|offset|text, the offset and text of the search lead back to its results (empty without a search).
    /// </summary>
    public const string Artist = "a";

    /// <summary>
    /// The full biography of an artist: mbid.
    /// </summary>
    public const string Biography = "b";

    /// <summary>
    /// The setlists of an artist, ten per screen: mbid|screen (from 1).
    /// </summary>
    public const string Setlists = "l";

    /// <summary>
    /// One setlist: setlist id|mbid|screen of the setlists to go back to.
    /// </summary>
    public const string Setlist = "t";

    /// <summary>
    /// Top tracks of an artist on Last.fm: mbid.
    /// </summary>
    public const string TopTracks = "o";

    /// <summary>
    /// Play a song, a new track message: mbid|song.
    /// </summary>
    public const string Play = "p";

    /// <summary>
    /// Play again from the "busy" message, which is deleted when the track is sent: mbid|song.
    /// </summary>
    public const string PlayAgain = "q";

    /// <summary>
    /// Show the lyrics in the track message: mbid|song.
    /// </summary>
    public const string ShowLyrics = "y";

    /// <summary>
    /// Hide the lyrics of the track message: mbid|song.
    /// </summary>
    public const string HideLyrics = "h";

    /// <summary>
    /// The start menu, no arguments.
    /// </summary>
    public const string Menu = "m";

    /// <summary>
    /// Help, no arguments.
    /// </summary>
    public const string Help = "w";

    /// <summary>
    /// About: version, sources, support. No arguments.
    /// </summary>
    public const string About = "i";
}
