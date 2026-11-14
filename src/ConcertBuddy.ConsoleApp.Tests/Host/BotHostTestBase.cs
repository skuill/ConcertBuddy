using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Text.Json.Nodes;

namespace ConcertBuddy.ConsoleApp.Tests.Host;

/// <summary>
/// The real host with a fake Telegram, captured logs and a fake clock.
/// </summary>
public abstract class BotHostTestBase
{
    protected const string TokenArgument = "--Configuration:TelegramToken=123456:test-token";
    protected const long TesterId = 987654321;
    protected static readonly TimeSpan SafetyTimeout = TimeSpan.FromSeconds(30);

    internal CapturingLoggerProvider Logs { get; } = new();

    internal CountingTimeProvider Time { get; } = new();

    protected static CancellationToken Token => TestContext.Current.CancellationToken;

    internal IHost CreateHost(FakeTelegram telegram, Action<IServiceCollection>? configureServices, params string[] arguments)
    {
        return Program.CreateHost(arguments.Prepend(TokenArgument).ToArray(), services =>
        {
            services.AddHttpClient(ServiceCollectionExtensions.TelegramClientName).ConfigurePrimaryHttpMessageHandler(() => telegram);
            services.AddSingleton<ILoggerProvider>(Logs);
            services.AddSingleton<TimeProvider>(Time);
            configureServices?.Invoke(services);
        });
    }

    internal IHost CreateHost(FakeTelegram telegram, params string[] arguments)
    {
        return CreateHost(telegram, null, arguments);
    }

    protected List<string> HandledLines()
    {
        return Logs.Entries.Where(x => x.Message.StartsWith("Handled update:")).Select(x => x.Message).ToList();
    }

    protected static async Task WaitUntil(Func<bool> condition, Func<string>? describe = null)
    {
        var deadline = DateTime.UtcNow + SafetyTimeout;
        while (!condition())
        {
            if (DateTime.UtcNow > deadline)
            {
                throw new TimeoutException(describe?.Invoke() ?? "The condition was not met in time.");
            }
            await Task.Delay(5, Token);
        }
    }

    protected static string CreateTextUpdate(int updateId, string text, string chatType = "private")
    {
        var message = new JsonObject
        {
            ["message_id"] = updateId,
            ["date"] = 1,
            ["chat"] = new JsonObject { ["id"] = 10, ["type"] = chatType, ["title"] = chatType == "private" ? null : "T" },
            ["from"] = new JsonObject { ["id"] = TesterId, ["is_bot"] = false, ["first_name"] = "Tester" },
            ["text"] = text,
        };
        return new JsonObject { ["update_id"] = updateId, ["message"] = message }.ToJsonString();
    }

    /// <summary>
    /// A press of a button with this data under the message the bot sent (<see cref="FakeTelegram.SentMessageId"/>).
    /// </summary>
    protected static string CreateCallbackUpdate(int updateId, string? data, string chatType = "private", JsonObject? richMessage = null)
    {
        var message = new JsonObject
        {
            ["message_id"] = FakeTelegram.SentMessageId,
            ["date"] = 1,
            ["chat"] = new JsonObject { ["id"] = 10, ["type"] = chatType, ["title"] = chatType == "private" ? null : "T" },
        };
        if (richMessage == null)
        {
            message["text"] = "x";
        }
        else
        {
            message["rich_message"] = richMessage;
        }
        var query = new JsonObject
        {
            ["id"] = $"q{updateId}",
            ["from"] = new JsonObject { ["id"] = TesterId, ["is_bot"] = false, ["first_name"] = "Tester" },
            ["chat_instance"] = "1",
            ["message"] = message,
        };
        if (data != null)
        {
            query["data"] = data;
        }
        return new JsonObject { ["update_id"] = updateId, ["callback_query"] = query }.ToJsonString();
    }
}
