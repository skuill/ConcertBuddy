using Microsoft.Extensions.Logging;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler;

/// <summary>
/// The command menu and the descriptions of the bot, set from code on every start, so they never drift from what
/// the bot can do. BotFather is needed only for the name, the photo and inline mode.
/// </summary>
public sealed class BotProfile
{
    /// <summary>
    /// "What can this bot do?" in an empty chat, at most 512 characters.
    /// </summary>
    public const string Description = "🎸 ConcertBuddy shows what artists play live: setlists of their concerts from setlist.fm, "
        + "every song to listen to and its lyrics.\n\nSend an artist or band name to start.";

    /// <summary>
    /// The profile page and the share card, at most 120 characters.
    /// </summary>
    public const string ShortDescription = "Setlists of concerts, songs to listen to and lyrics. Send an artist name.";

    public static readonly IReadOnlyList<BotCommand> Commands = new[]
    {
        new BotCommand { Command = "search", Description = "Find an artist: /search Metallica" },
        new BotCommand { Command = "help", Description = "How to use the bot" },
        new BotCommand { Command = "about", Description = "Sources, code and support" },
    };

    private readonly ITelegramBotClient _botClient;
    private readonly ILogger<BotProfile> _logger;

    public BotProfile(ITelegramBotClient botClient, ILogger<BotProfile> logger)
    {
        _botClient = botClient;
        _logger = logger;
    }

    public async Task SetUpAsync(CancellationToken cancellationToken)
    {
        try
        {
            await _botClient.SetMyCommands(Commands, cancellationToken: cancellationToken);
            // In groups the menu shows the same commands: there the bot sees only commands, so they matter even more.
            await _botClient.SetMyCommands(Commands, new BotCommandScopeAllGroupChats(), cancellationToken: cancellationToken);
            await _botClient.SetMyDescription(Description, cancellationToken: cancellationToken);
            await _botClient.SetMyShortDescription(ShortDescription, cancellationToken: cancellationToken);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            // The bot works without them, with the menu and texts of the last start.
            _logger.LogWarning(exception, "Can't set the command menu and descriptions of the bot");
        }
    }
}
