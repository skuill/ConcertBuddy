using ConcertBuddy.ConsoleApp.TelegramBot.Command.Abstract;
using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using MusicSearcher;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.InlineQueryResults;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Command;

/// <summary>
/// Inline mode: "Send to a chat" under a setlist types <c>@bot s_&lt;setlist id&gt;</c> in the chosen chat, and the
/// answer is the setlist card to send there. Any other inline query gets no results for now (#14).
/// </summary>
public class ShareCommand : AbstractCommand<bool, InlineQuery>
{
    /// <summary>
    /// How long Telegram may keep the answer for the same query: a setlist rarely changes within minutes.
    /// </summary>
    public const int CacheSeconds = 300;

    private readonly SetlistScreens _screens;

    public ShareCommand(IMusicSearcherClient musicSearcherClient, ITelegramBotClient telegramBotClient, SetlistScreens screens, InlineQuery data)
        : base(musicSearcherClient, telegramBotClient, data)
    {
        _screens = screens;
    }

    /// <summary>
    /// setlist.fm ids are short hexadecimal strings; this also keeps a start parameter within its 64 allowed characters.
    /// </summary>
    public static bool IsSetlistId(string id)
    {
        return id.Length is > 0 and <= 32 && id.All(char.IsAsciiHexDigit);
    }

    public override async Task<bool> ExecuteAsync()
    {
        var query = Data.Query.Trim();
        var setlistId = query.StartsWith(SetlistScreens.SharePrefix, StringComparison.Ordinal) ? query[SetlistScreens.SharePrefix.Length..] : string.Empty;
        if (!IsSetlistId(setlistId))
        {
            Result = CommandResult.NotFound;
            await TelegramBotClient.AnswerInlineQuery(Data.Id, Array.Empty<InlineQueryResult>(), cacheTime: CacheSeconds);
            return true;
        }

        SharedCard? card;
        try
        {
            card = await _screens.SharedCardAsync(setlistId);
        }
        catch (MusicServiceUnavailableException)
        {
            // No cache: the next try may find setlist.fm back.
            Result = CommandResult.Unavailable;
            await TelegramBotClient.AnswerInlineQuery(Data.Id, Array.Empty<InlineQueryResult>(), cacheTime: 0);
            return true;
        }

        if (card == null)
        {
            Result = CommandResult.NotFound;
            await TelegramBotClient.AnswerInlineQuery(Data.Id, Array.Empty<InlineQueryResult>(), cacheTime: CacheSeconds);
            return true;
        }

        var result = new InlineQueryResultArticle(setlistId, card.Title, new InputRichMessageContent(new InputRichMessage { Html = card.Html }))
        {
            Description = card.Description,
            ReplyMarkup = card.Keyboard,
        };
        await TelegramBotClient.AnswerInlineQuery(Data.Id, new InlineQueryResult[] { result }, cacheTime: CacheSeconds);
        return true;
    }
}
