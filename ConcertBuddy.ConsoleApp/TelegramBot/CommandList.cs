namespace ConcertBuddy.ConsoleApp.TelegramBot
{
    public class CommandList
    {
        public const string COMMAND_START = "/start";
        public const string COMMAND_SEARCH = "/search";
        public const string COMMAND_ARTIST = "/artist";
        public const string COMMAND_BIOGRAPHY = "/biography";
        public const string COMMAND_SETLISTS = "/setlists";
        public const string COMMAND_SETLIST = "/setlist";
        public const string COMMAND_TRACK = "/track";
        public const string COMMAND_DELETE = "/delete";

        // Command with mbid. Example: /artist b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
        public const string CALLBACK_DATA_FORMAT_ARTIST = $"{COMMAND_ARTIST} {{0}}";
        // Command with mbid. Example: /biography b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
        public const string CALLBACK_DATA_FORMAT_BIOGRAPHY = $"{COMMAND_BIOGRAPHY} {{0}}";
        // Command with mbid. Example: /setlists b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
        public const string CALLBACK_DATA_FORMAT_SETLISTS = $"{COMMAND_SETLISTS} {{0}}";
        // Command with mbid. Example: /setlists b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
        public const string CALLBACK_DATA_FORMAT_SETLIST = $"{COMMAND_SETLIST} {{0}} {{1}}";
        // Command with mbid. Example: /setlists b10bbbfc-cf9e-42e0-be17-e2c3e1d2600d
        public const string CALLBACK_DATA_FORMAT_TRACK = $"{COMMAND_TRACK} {{0}}";
        // Command with mbid. Example: /delete
        public const string CALLBACK_DATA_FORMAT_DELETE = $"{COMMAND_DELETE}";
    }
}
