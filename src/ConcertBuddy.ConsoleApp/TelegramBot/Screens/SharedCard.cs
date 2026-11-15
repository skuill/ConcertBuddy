using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// A setlist sent to another chat through inline mode: the title and description of the inline result, the rich
/// HTML of the card, the button that opens the setlist in the bot (null while the bot does not know its username).
/// </summary>
public sealed record SharedCard(string Title, string Description, string Html, InlineKeyboardMarkup? Keyboard);
