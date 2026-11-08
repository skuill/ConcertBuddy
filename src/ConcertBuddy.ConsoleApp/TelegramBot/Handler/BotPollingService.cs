using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Polling;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler;

/// <summary>
/// Receives the updates from Telegram until the application stops.
/// </summary>
public sealed class BotPollingService : BackgroundService
{
    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly ILogger<BotPollingService> _logger;

    public BotPollingService(
        ITelegramBotClient botClient,
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        ILogger<BotPollingService> logger)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
        _environment = environment;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // One scope for the whole run: the music clients keep their caches and sessions as long as the bot works.
        using var scope = _scopeFactory.CreateScope();
        var botHandlers = scope.ServiceProvider.GetRequiredService<IBotHandlers>();

        var botUser = await _botClient.GetMe(stoppingToken);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        _logger.LogInformation(
            "Bot {BotName} [{BotId}] start listening. Version: [{Version}]. Environment: [{Environment}]",
            botUser.FirstName,
            botUser.Id,
            version,
            _environment.EnvironmentName);

        await _botClient.ReceiveAsync(
            botHandlers.HandleUpdateAsync,
            botHandlers.HandleErrorAsync,
            new ReceiverOptions(),
            stoppingToken);
    }
}
