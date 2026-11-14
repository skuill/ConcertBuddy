namespace ConcertBuddy.ConsoleApp.TelegramBot;

public class CommandList
{
    public const string COMMAND_START = "/start";
    public const string COMMAND_HELP = "/help";
    public const string COMMAND_ABOUT = "/about";
    public const string COMMAND_SEARCH = "/search";
    public const string COMMAND_ARTIST = "/artist";
    public const string COMMAND_BIOGRAPHY = "/biography";
    public const string COMMAND_SETLISTS = "/setlists";
    public const string COMMAND_SETLIST = "/setlist";
    public const string COMMAND_TRACK = "/track";
    public const string COMMAND_LYRIC = "/lyric";
    public const string COMMAND_DELETE = "/delete";
    public const string COMMAND_TOP = "/top";

    // The names in the log line of the updates; of the button data only "/delete" is still a command (#48).
    public const string CALLBACK_DATA_FORMAT_DELETE = $"{COMMAND_DELETE}";
}
