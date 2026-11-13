using Telegram.Bot.Types.ReplyMarkups;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Screens;

/// <summary>
/// What one bot message shows: HTML text, buttons and an optional link preview shown above the text.
/// A rich screen is a rich message (tables, headings, buttons in the text) and has no link preview.
/// A flow keeps one message and replaces its screen (#48).
/// </summary>
public sealed record Screen(string Html, InlineKeyboardMarkup Keyboard, string? PreviewUrl = null, bool IsRich = false);
