using MusicSearcher.Model.Abstract;
using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Helper;

public static class InlineKeyboardHelper
{
    private const int CALLBACK_DATA_MAX_BYTES = 64;

    public static InlineKeyboardMarkup GetLyricInlineKeyboardMenu(string mbid, string songName)
    {
        List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();
        inlineKeyboardButtons.Add(new[] {
            InlineKeyboardButton.WithCallbackData("🎼 Lyric", GetCallbackDataWithName(CommandList.CALLBACK_DATA_FORMAT_LYRIC, mbid, songName)),
        });
        return new InlineKeyboardMarkup(inlineKeyboardButtons).WithDeleteButton();
    }

    public static InlineKeyboardMarkup GetTracksInlineKeyboardMenu(MusicTrackBase[] tracks, string mbid)
    {
        List<InlineKeyboardButton[]> inlineKeyboardButtons = new List<InlineKeyboardButton[]>();
        int counter = 1;
        foreach (var track in tracks)
        {
            string callbackText = $"{counter++}. {track.TrackName}";
            string callbackData = GetCallbackDataWithName(CommandList.CALLBACK_DATA_FORMAT_TRACK, mbid, track.TrackName!);
            inlineKeyboardButtons.Add(new[] { InlineKeyboardButton.WithCallbackData(callbackText, callbackData) });
        }
        return new InlineKeyboardMarkup(inlineKeyboardButtons);
    }

    /// <summary>
    /// The old "/track" command for a song; the track becomes a screen in the next step of #48.
    /// </summary>
    public static string GetTrackCallbackData(string mbid, string songName)
    {
        return GetCallbackDataWithName(CommandList.CALLBACK_DATA_FORMAT_TRACK, mbid, songName);
    }

    private static string GetCallbackDataWithName(string format, string mbid, string name)
    {
        // Substring because:
        // callback_data (String) - Optional.Data to be sent in a callback query to the bot when button is pressed, 1 - 64 bytes    
        // There is problem with encoding. For example: "/track 827e1b52-1782-4e73-b8e7-a7b6939370f5 Стволок за поясок"
        // string.Length is 61. But byte size in UTF-8 encoding is 76!
        // https://core.telegram.org/bots/api#making-requests . All queries must be made using UTF-8.
        string callbackData = string.Format(format, mbid, name);
        return StringHelper.SubstringByByteLength(callbackData, CALLBACK_DATA_MAX_BYTES);
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
