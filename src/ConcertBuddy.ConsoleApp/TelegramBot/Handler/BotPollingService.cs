using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using System.Collections.Concurrent;
using System.Reflection;
using Telegram.Bot;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.TelegramBot.Handler;

/// <summary>
/// Receives the updates from Telegram until the application stops. An outage takes one line in the log,
/// and the next line says when the connection is back, so the loop is here and not in the Telegram.Bot library.
/// </summary>
public sealed class BotPollingService : BackgroundService
{
    private const int PollingTimeoutSeconds = 30;
    private const double MaxDelaySeconds = 60;
    private const int UnauthorizedErrorCode = 401;
    private const int TooManyRequestsErrorCode = 429;
    private const int FirstServerErrorCode = 500;

    // Several updates at once: a slow command (a track download, a lyrics search) must not hold the other users.
    internal const int MaxParallelUpdates = 8;

    private readonly ITelegramBotClient _botClient;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostEnvironment _environment;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<BotPollingService> _logger;

    private readonly SemaphoreSlim _slots = new(MaxParallelUpdates);
    private readonly ConcurrentDictionary<Task, byte> _running = new();

    private int _failedAttempts;

    public BotPollingService(
        ITelegramBotClient botClient,
        IServiceScopeFactory scopeFactory,
        IHostEnvironment environment,
        TimeProvider timeProvider,
        ILogger<BotPollingService> logger)
    {
        _botClient = botClient;
        _scopeFactory = scopeFactory;
        _environment = environment;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        // One scope for the whole run: the music clients keep their caches and sessions as long as the bot works.
        using var scope = _scopeFactory.CreateScope();
        var botHandlers = scope.ServiceProvider.GetRequiredService<IBotHandlers>();
        // Created now and not with the first message: the Yandex Music client signs in when it is created.
        scope.ServiceProvider.GetRequiredService<IMusicSearcherClient>();

        try
        {
            await PollAsync(botHandlers, stoppingToken);
        }
        finally
        {
            // The scope and its clients stay alive until the updates in progress are handled.
            await Task.WhenAll(_running.Keys);
        }
    }

    private async Task PollAsync(IBotHandlers botHandlers, CancellationToken stoppingToken)
    {
        var isStarted = false;
        int? offset = null;
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                if (!isStarted)
                {
                    await LogStartAsync(stoppingToken);
                    isStarted = true;
                }

                var updates = await _botClient.GetUpdates(offset, timeout: PollingTimeoutSeconds, cancellationToken: stoppingToken);
                OnPollingSucceeded();

                foreach (var update in updates)
                {
                    offset = update.Id + 1;
                    await _slots.WaitAsync(stoppingToken);
                    Start(HandleAsync(botHandlers, update, stoppingToken));
                }
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                return;
            }
            catch (Exception exception) when (exception is not ApiRequestException { ErrorCode: UnauthorizedErrorCode })
            {
                await Task.Delay(OnPollingFailed(exception), _timeProvider, stoppingToken);
            }
        }
    }

    private void Start(Task task)
    {
        _running.TryAdd(task, 0);
        task.ContinueWith(finished => _running.TryRemove(finished, out _), TaskScheduler.Default);
    }

    private async Task HandleAsync(IBotHandlers botHandlers, Update update, CancellationToken stoppingToken)
    {
        try
        {
            // Off the polling loop at once: the loop asks Telegram for the next updates while this one is handled.
            await Task.Yield();
            await botHandlers.HandleUpdateAsync(update, stoppingToken);
        }
        finally
        {
            _slots.Release();
        }
    }

    private async Task LogStartAsync(CancellationToken stoppingToken)
    {
        var botUser = await _botClient.GetMe(stoppingToken);
        var version = Assembly.GetExecutingAssembly().GetName().Version;
        _logger.LogInformation(
            "Bot {BotName:l} [{BotId}] start listening. Version: [{Version}]. Environment: [{Environment}]",
            botUser.FirstName,
            botUser.Id,
            version,
            _environment.EnvironmentName);
    }

    private void OnPollingSucceeded()
    {
        if (_failedAttempts > 0)
        {
            _logger.LogInformation("Telegram polling works again after {FailedAttempts} failed attempts", _failedAttempts);
            _failedAttempts = 0;
        }
    }

    private TimeSpan OnPollingFailed(Exception exception)
    {
        var delay = GetDelay(exception);
        _failedAttempts++;

        var level = _failedAttempts > 1 ? LogLevel.Debug : IsTemporary(exception) ? LogLevel.Warning : LogLevel.Error;
        _logger.Log(level, "Telegram polling failed: {Reason}. Next attempt in {DelaySeconds} s", exception.Message, delay.TotalSeconds);
        return delay;
    }

    private TimeSpan GetDelay(Exception exception)
    {
        if (exception is ApiRequestException { Parameters.RetryAfter: int retryAfter })
        {
            return TimeSpan.FromSeconds(retryAfter);
        }
        return TimeSpan.FromSeconds(Math.Min(MaxDelaySeconds, Math.Pow(2, _failedAttempts)));
    }

    private static bool IsTemporary(Exception exception)
    {
        return exception is not ApiRequestException api
            || api.ErrorCode == TooManyRequestsErrorCode
            || api.ErrorCode >= FirstServerErrorCode;
    }
}
