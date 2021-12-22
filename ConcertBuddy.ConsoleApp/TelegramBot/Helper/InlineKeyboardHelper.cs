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

        public static InlineKeyboardMarkup GetArtistsInlineKeyboard(IEnumerable<MusicArtist> artists)
        {
            List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();
            int counter = 1;
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
            inlineKeyboardButtons = inlineKeyboardButtons.Append(new List<InlineKeyboardButton> { InlineKeyboardButton.WithCallbackData("❌", string.Format(CommandList.CALLBACK_DATA_FORMAT_DELETE)) });
            return new InlineKeyboardMarkup(inlineKeyboardButtons);
        }
    }
}
