using ConcertBuddy.ConsoleApp.Search;
using MusicSearcher.Model;
using SetlistFmAPI.Models;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Helper
{
    public static class InlineKeyboardHelper
    {
        public static InlineKeyboardMarkup GetTracksInlineKeyboardMenu(Set set, string mbid)
        {
            List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();
            int counter = 1;
            foreach (var song in set.Songs)
            {
                string callbackText = $"{counter++}. {song.ToString()}";
                // substring because:
                //callback_data String  Optional.Data to be sent in a callback query to the bot when button is pressed, 1 - 64 bytes                
                string callbackData = string.Format(CommandList.CALLBACK_DATA_FORMAT_TRACK, mbid, song.Name);
                if (callbackData.Length > 64)
                    callbackData = callbackData.Substring(0, 64);
                inlineKeyboardButtons.Add(new[] { InlineKeyboardButton.WithCallbackData(callbackText, callbackData) });
            }
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }

        public static InlineKeyboardMarkup GetSetlistsInlineKeyboardMenu(IEnumerable<Setlist> setlists)
        {
            List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();
            foreach (var setlist in setlists)
            {
                string callbackText = $"{setlist.ToString()}";
                string callbackData = string.Format(CommandList.CALLBACK_DATA_FORMAT_SETLIST, setlist.Artist.MBID, setlist.Id);
                inlineKeyboardButtons.Add(new[] { InlineKeyboardButton.WithCallbackData(callbackText, callbackData) });
            }
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }

        public static InlineKeyboardMarkup GetArtistsInlineKeyboard(IEnumerable<MusicArtist> artists, int counter = 1)
        {
            List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();
            foreach (var artist in artists)
            {
                string callbackText = $"{counter++}. {artist.Name} ({artist.ActiveYears}) [{artist.Area}]";
                string callbackData = string.Format(CommandList.CALLBACK_DATA_FORMAT_ARTIST, artist.MBID);
                inlineKeyboardButtons.Add(new[] { InlineKeyboardButton.WithCallbackData(callbackText, callbackData) });
            }
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }

        public static InlineKeyboardMarkup GetArtistInlineKeyboardMenu(string mbid)
        {
            List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();
            inlineKeyboardButtons.Add(new[] {
                InlineKeyboardButton.WithCallbackData("🎓 Biography", string.Format(CommandList.CALLBACK_DATA_FORMAT_BIOGRAPHY, mbid)),
                InlineKeyboardButton.WithCallbackData("📝 Setlists", string.Format(CommandList.CALLBACK_DATA_FORMAT_SETLISTS, SearchConstants.SEARCH_SETLISTS_PAGE_DEFAULT, 0, mbid)),
            });
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }

        public static InlineKeyboardMarkup WithNavigationButtons(this InlineKeyboardMarkup inlineKeyboardMarkup, string commandFormat, string data, int page, int limit = 0)
        {
            var inlineKeyboard = inlineKeyboardMarkup.InlineKeyboard;
            var navigationButtons = new List<InlineKeyboardButton>();
            int shift = limit == 0 ? 1 : limit;
            
            if (page - shift >= 0 && !(limit == 0 && page == 1))
                navigationButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️", string.Format(commandFormat, page - shift, limit, data)));
            navigationButtons.Add(GetDeleteButton());
            navigationButtons.Add(InlineKeyboardButton.WithCallbackData("➡️", string.Format(commandFormat, page + shift, limit, data)));

            inlineKeyboard = inlineKeyboard.Append(navigationButtons);
            return new InlineKeyboardMarkup(inlineKeyboard);
        }

        public static InlineKeyboardMarkup WithDeleteButton(this InlineKeyboardMarkup inlineKeyboardMarkup, params int[] messageIds)
        {
            var inlineKeyboardButtons = inlineKeyboardMarkup.InlineKeyboard;
            inlineKeyboardButtons = inlineKeyboardButtons.Append(new List<InlineKeyboardButton> { GetDeleteButton(messageIds) });
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }

        private static InlineKeyboardButton GetDeleteButton(params int[] messageIds)
        {
            if (messageIds != null && messageIds.Any())
            {
                string command = CommandList.COMMAND_DELETE + " " + string.Join(" ", messageIds.Select(x => x.ToString()));
                return InlineKeyboardButton.WithCallbackData("❌", command);
            }
            return InlineKeyboardButton.WithCallbackData("❌", string.Format(CommandList.CALLBACK_DATA_FORMAT_DELETE));
        }
    }
}
