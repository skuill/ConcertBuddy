using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using ConcertBuddy.ConsoleApp.Tests.Host;
using ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.Scenarios;

/// <summary>
/// A screen that takes more than a second shows "⏳ Loading…" without buttons first (#94), so the user sees that the tap
/// works and does not tap again; a fast one goes straight to the screen.
/// </summary>
public class LoadingScenarioTest : BotHostTestBase
{
    private const string Mbid = TestArtist.MetallicaMbid;

    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();
    private readonly TaskCompletionSource _started = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _release = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public LoadingScenarioTest()
    {
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).ReturnsLazily(SlowArtistAsync);
    }

    [Fact]
    public async Task Artist_CardTakesMoreThanASecond_LoadingWithoutButtonsThenTheCard()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"a|{Mbid}|0|Metallica"));
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await _started.Task.WaitAsync(SafetyTimeout, Token);

        // Act: just under a second nothing changes, at a second the message says it is loading.
        Time.Advance(ScreenEdit.LoadingDelay - TimeSpan.FromMilliseconds(1));
        await Task.Delay(50, Token);
        var editsBefore = telegram.RequestsOf("editMessageText").Count;
        Time.Advance(TimeSpan.FromMilliseconds(1));
        await WaitUntil(() => telegram.RequestsOf("editMessageText").Count == 1);
        _release.SetResult();
        await WaitUntil(() => HandledLines().Count == 1);
        await host.StopAsync(Token);

        // Assert
        editsBefore.ShouldBe(0);
        telegram.Requests.Select(x => x.Method).ShouldBe(new[] { "answerCallbackQuery", "editMessageText", "editMessageText" });
        var edits = telegram.RequestsOf("editMessageText");
        edits[0]["text"]!.GetValue<string>().ShouldBe("⏳ Loading…");
        edits[0]["reply_markup"]!["inline_keyboard"]!.AsArray().ShouldBeEmpty();
        edits[1]["text"]!.GetValue<string>().ShouldBe("🇺🇸 <b>Metallica</b>\nGroup · United States · 1981 -");
        HandledLines().Single().ShouldContain(": Found in ");
    }

    [Fact]
    public async Task Artist_CardWithinASecond_StraightToTheCard()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"a|{Mbid}|0|Metallica"));
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await _started.Task.WaitAsync(SafetyTimeout, Token);

        // Act
        Time.Advance(ScreenEdit.LoadingDelay / 2);
        _release.SetResult();
        await WaitUntil(() => HandledLines().Count == 1);
        Time.Advance(ScreenEdit.LoadingDelay);
        await host.StopAsync(Token);

        // Assert
        telegram.RequestsOf("editMessageText").Select(x => x["text"]!.GetValue<string>()).ShouldBe(new[] { "🇺🇸 <b>Metallica</b>\nGroup · United States · 1981 -" });
    }

    private async Task<MusicArtistBase> SlowArtistAsync()
    {
        _started.TrySetResult();
        await _release.Task;
        return new MusicArtist(TestArtist.Metallica());
    }
}
