using System.Collections.Concurrent;
using System.Net;
using System.Text;
using System.Text.Json.Nodes;

namespace ConcertBuddy.ConsoleApp.Tests.Host;

/// <summary>
/// The Telegram Bot API for the host tests: gives the queued updates to the poller and remembers what the bot sends.
/// </summary>
internal sealed class FakeTelegram : HttpMessageHandler
{
    public const int SentMessageId = 100;

    private const string Message = """{"message_id":100,"date":1,"chat":{"id":10,"type":"private"}}""";

    private readonly ConcurrentQueue<string> _updates = new();
    private readonly SemaphoreSlim _updateArrived = new(0);
    private readonly ConcurrentQueue<string> _answers = new();
    private readonly ConcurrentQueue<(string Method, JsonNode Body)> _requests = new();
    private int _failedAnswers;
    private int _answersStarted;
    private int _pollCalls;

    public FakeTelegram(params string[] updates)
    {
        foreach (var update in updates)
        {
            AddUpdate(update);
        }
    }

    public int FailedAnswers { init => _failedAnswers = value; }

    public Func<int, Task> BeforeAnswer { get; init; } = _ => Task.CompletedTask;

    public int AnswersStarted => Volatile.Read(ref _answersStarted);

    public Func<HttpResponseMessage> GetMeResponse { get; init; } = () => Ok("""{"id":123456,"is_bot":true,"first_name":"TestBot","username":"test_bot"}""");

    public Func<HttpResponseMessage> EditResponse { get; set; } = () => Ok(Message);

    public ConcurrentQueue<Func<HttpResponseMessage>> PollingFailures { get; } = new();

    /// <summary>
    /// The texts of sendMessage.
    /// </summary>
    public IReadOnlyCollection<string> Answers => _answers;

    /// <summary>
    /// Every request but getMe and getUpdates, in order, with its JSON body.
    /// </summary>
    public IReadOnlyCollection<(string Method, JsonNode Body)> Requests => _requests;

    public int PollCalls => Volatile.Read(ref _pollCalls);

    public bool PollingStopped { get; private set; }

    public void AddUpdate(string update)
    {
        _updates.Enqueue(update);
        _updateArrived.Release();
    }

    public List<JsonNode> RequestsOf(string method)
    {
        return Requests.Where(x => x.Method == method).Select(x => x.Body).ToList();
    }

    public static HttpResponseMessage Json(HttpStatusCode status, string json)
    {
        return new HttpResponseMessage(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };
    }

    public static HttpResponseMessage Ok(string result) => Json(HttpStatusCode.OK, $$"""{"ok":true,"result":{{result}}}""");

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var method = request.RequestUri!.Segments.Last();
        switch (method)
        {
            case "getMe":
                return GetMeResponse();
            case "getUpdates":
                return await GetUpdates(cancellationToken);
        }

        var body = await ReadBodyAsync(request.Content, cancellationToken);
        if (ProfileMethods.Contains(method))
        {
            // The bot sets its menu and descriptions on start: kept apart, so the chat scenarios see only their requests.
            _profileRequests.Enqueue((method, body));
            return ProfileResponse();
        }

        _requests.Enqueue((method, body));
        switch (method)
        {
            case "sendMessage":
                return await SendMessage(body.ToJsonString());
            case "sendRichMessage":
                return SendRichResponse();
            case "editMessageText":
                return EditResponse();
            case "deleteMessage":
                return DeleteResponse();
            case "answerCallbackQuery":
            case "sendChatAction":
                return Ok("true");
            default:
                throw new InvalidOperationException($"Unexpected Telegram method {method}");
        }
    }

    public Func<HttpResponseMessage> SendRichResponse { get; set; } = () => Ok(Message);

    public Func<HttpResponseMessage> ProfileResponse { get; set; } = () => Ok("true");

    /// <summary>
    /// setMyCommands, setMyDescription and setMyShortDescription, in order.
    /// </summary>
    public IReadOnlyCollection<(string Method, JsonNode Body)> ProfileRequests => _profileRequests;

    private static readonly HashSet<string> ProfileMethods = new() { "setMyCommands", "setMyDescription", "setMyShortDescription" };

    private readonly ConcurrentQueue<(string Method, JsonNode Body)> _profileRequests = new();

    public Func<HttpResponseMessage> DeleteResponse { get; set; } = () => Ok("true");

    /// <summary>
    /// A JSON request as it is; an upload (multipart form) as an object of its fields, a file as its name and size.
    /// </summary>
    private static async Task<JsonNode> ReadBodyAsync(HttpContent? content, CancellationToken cancellationToken)
    {
        if (content is not MultipartFormDataContent form)
        {
            return JsonNode.Parse(content == null ? "{}" : await content.ReadAsStringAsync(cancellationToken))!;
        }

        var fields = new JsonObject();
        foreach (var part in form)
        {
            var name = part.Headers.ContentDisposition?.Name?.Trim('"') ?? string.Empty;
            var fileName = part.Headers.ContentDisposition?.FileName?.Trim('"') ?? part.Headers.ContentDisposition?.FileNameStar;
            if (fileName != null)
            {
                var bytes = await part.ReadAsByteArrayAsync(cancellationToken);
                fields[name] = new JsonObject { ["file_name"] = fileName, ["size"] = bytes.Length };
                continue;
            }

            var value = await part.ReadAsStringAsync(cancellationToken);
            try
            {
                fields[name] = JsonNode.Parse(value);
            }
            catch (System.Text.Json.JsonException)
            {
                fields[name] = value;
            }
        }
        return fields;
    }

    private async Task<HttpResponseMessage> GetUpdates(CancellationToken cancellationToken)
    {
        Interlocked.Increment(ref _pollCalls);
        if (PollingFailures.TryDequeue(out var failure))
        {
            return failure();
        }

        try
        {
            await _updateArrived.WaitAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            PollingStopped = true;
            throw;
        }

        _updates.TryDequeue(out var update);
        return Ok($"[{update}]");
    }

    private async Task<HttpResponseMessage> SendMessage(string content)
    {
        await BeforeAnswer(Interlocked.Increment(ref _answersStarted));
        _answers.Enqueue(content);
        if (Interlocked.Decrement(ref _failedAnswers) >= 0)
        {
            return Json(HttpStatusCode.BadRequest, """{"ok":false,"error_code":400,"description":"Bad Request: test failure"}""");
        }
        return Ok(Message);
    }
}
