namespace ConcertBuddy.ConsoleApp.TelegramBot;

public class CommandList
{
    public const string COMMAND_START = "/start";
    public const string COMMAND_SEARCH = "/search";
    public const string COMMAND_ARTIST = "/artist";
    public const string COMMAND_BIOGRAPHY = "/biography";
    public const string COMMAND_SETLISTS = "/setlists";
    public const string COMMAND_SETLIST = "/setlist";
    public const string COMMAND_TRACK = "/track";
    public const string COMMAND_LYRIC = "/lyric";
    public const string COMMAND_DELETE = "/delete";
    public const string COMMAND_TOP = "/top";

    // WARNING: callback_data - Data to be sent in a callback query to the bot when button is pressed, 1-64 bytes

    /// <summary>
    /// Search track by artist mbid and name. Example: /track b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d Some track name
    /// Args:
    /// 0 - artist MBID
    /// 1 - track's name (from setlist.fm)
    /// </summary>
    public const string CALLBACK_DATA_FORMAT_TRACK = $"{COMMAND_TRACK} {{0}} {{1}}";

    /// <summary>
    /// Search lyric by artist mbid and track name. Example: /lyric b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d Some track name
    /// Args:
    /// 0 - artist MBID
    /// 1 - track's name (from setlist.fm)
    /// </summary>
    public const string CALLBACK_DATA_FORMAT_LYRIC = $"{COMMAND_LYRIC} {{0}} {{1}}";

    // Command with mbid. Example: /delete
    public const string CALLBACK_DATA_FORMAT_DELETE = $"{COMMAND_DELETE}";

    /// <summary>
    /// Get top of something (tracks, albums, artists, ..). Example: /top track b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
    /// Args:
    /// 0 - type of top (track, album, artist, ..).
    /// 1..N - top's params. For track it's artist's mbid.
    /// </summary>
    public const string CALLBACK_DATA_FORMAT_TOP = $"{COMMAND_TOP} {{0}} {{1}}";
}
