using MusicSearcher.Model;
using SetlistFmAPI.Models;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Helper
{
    public static class InlineKeyboardHelper
    {
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
                InlineKeyboardButton.WithCallbackData("📝 Setlists", string.Format(CommandList.CALLBACK_DATA_FORMAT_SETLISTS, mbid)),
            });
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }

        public static InlineKeyboardMarkup WithDeleteButton(this InlineKeyboardMarkup inlineKeyboardMarkup)
        {
            var inlineKeyboardButtons = inlineKeyboardMarkup.InlineKeyboard;
            inlineKeyboardButtons = inlineKeyboardButtons.Append(new List<InlineKeyboardButton> { GetDeleteButton() });
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }

        public static InlineKeyboardMarkup WithNavigationButtons(this InlineKeyboardMarkup inlineKeyboardMarkup, string command, string data, int page, int limit = 0)
        {
            var inlineKeyboard = inlineKeyboardMarkup.InlineKeyboard;
            var navigationButtons = new List<InlineKeyboardButton>();
            int shift = limit == 0 ? 1 : limit;
            
            if (page - shift >= 0 && !(limit == 0 && page == 1))
                navigationButtons.Add(InlineKeyboardButton.WithCallbackData("⬅️", string.Format(command, page - shift, limit, data)));
            navigationButtons.Add(GetDeleteButton());
            navigationButtons.Add(InlineKeyboardButton.WithCallbackData("➡️", string.Format(command, page + shift, limit, data)));

            inlineKeyboard = inlineKeyboard.Append(navigationButtons);
            return new InlineKeyboardMarkup(inlineKeyboard);
        }

        private static InlineKeyboardButton GetDeleteButton()
        {
            return InlineKeyboardButton.WithCallbackData("❌", string.Format(CommandList.CALLBACK_DATA_FORMAT_DELETE));
        }
    }
}
