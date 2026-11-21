using ConcertBuddy.ConsoleApp.TelegramBot.Screens;
using ConcertBuddy.ConsoleApp.Tests.Host;
using ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using Shouldly;

namespace ConcertBuddy.ConsoleApp.Tests.Scenarios;

/// <summary>
/// The latest tap on a message wins (#92): a tap that waits for MusicBrainz retries must not replace the screen of a newer tap.
/// </summary>
public class LatestTapScenarioTest : BotHostTestBase
{
    private const string Mbid = TestArtist.MetallicaMbid;
    private const string ArtistData = $"a|{Mbid}|0|Metallica";
    private const string Card = "🇺🇸 <b>Metallica</b>\nGroup · United States · 1981 -";
    private const string Busy = "⚠️ MusicBrainz is busy or down right now. It is not your query: try again in a minute.";
    private const int OtherMessageId = FakeTelegram.SentMessageId + 1;

    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();
    private readonly TaskCompletionSource _slowTapStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
    private readonly TaskCompletionSource _releaseSlowTap = new(TaskCreationOptions.RunContinuationsAsynchronously);

    public LatestTapScenarioTest()
    {
        // The first call waits like MusicBrainz retries and ends busy, the next one finds the artist at once.
        A.CallTo(() => _music.SearchArtistByMBID(Mbid))
            .ReturnsLazily(SlowBusyAsync).Once()
            .Then.Returns(new MusicArtist(TestArtist.Metallica()));
    }

    [Fact]
    public async Task Artist_SlowTapThenNewerTapOnTheSameMessage_NewerCardStaysAndTheOlderScreenIsDropped()
    {
        // Act
        var (telegram, versions) = await RunSlowTapThenNewerTap(FakeTelegram.SentMessageId);

        // Assert
        telegram.Requests.Select(x => x.Method).ShouldBe(new[] { "answerCallbackQuery", "answerCallbackQuery", "editMessageText" });
        var card = telegram.RequestsOf("editMessageText").Single();
        card["message_id"]!.GetValue<int>().ShouldBe(FakeTelegram.SentMessageId);
        card["text"]!.GetValue<string>().ShouldBe(Card);
        HandledLines().Count.ShouldBe(2);
        HandledLines()[0].ShouldStartWith("Handled update: CallbackQuery /artist in Private chat from user -: Found in ");
        HandledLines()[1].ShouldStartWith("Handled update: CallbackQuery /artist in Private chat from user -: Superseded in ");
        Logs.Entries.ShouldNotContain(x => x.Level >= LogLevel.Warning);
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).MustHaveHappenedTwiceExactly();
        versions.Count.ShouldBe(0);
    }

    [Fact]
    public async Task Artist_SlowTapThenTapOnAnotherMessage_BothMessagesGetTheirScreens()
    {
        // Act
        var (telegram, versions) = await RunSlowTapThenNewerTap(OtherMessageId);

        // Assert
        telegram.Requests.Select(x => x.Method).ShouldBe(new[] { "answerCallbackQuery", "answerCallbackQuery", "editMessageText", "editMessageText" });
        var edits = telegram.RequestsOf("editMessageText");
        edits[0]["message_id"]!.GetValue<int>().ShouldBe(OtherMessageId);
        edits[0]["text"]!.GetValue<string>().ShouldBe(Card);
        edits[1]["message_id"]!.GetValue<int>().ShouldBe(FakeTelegram.SentMessageId);
        edits[1]["text"]!.GetValue<string>().ShouldBe(Busy);
        HandledLines().Count.ShouldBe(2);
        HandledLines()[0].ShouldContain(": Found in ");
        HandledLines()[1].ShouldContain(": Unavailable in ");
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).MustHaveHappenedTwiceExactly();
        versions.Count.ShouldBe(0);
    }

    /// <summary>
    /// The first tap on the bot's message waits in MusicBrainz; a second tap on the given message ends first, then the first one ends.
    /// </summary>
    private async Task<(FakeTelegram Telegram, ScreenVersions Versions)> RunSlowTapThenNewerTap(int secondMessageId)
    {
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, ArtistData));
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await _slowTapStarted.Task.WaitAsync(SafetyTimeout, Token);

        telegram.AddUpdate(CreateCallbackUpdate(2, ArtistData, messageId: secondMessageId));
        await WaitUntil(() => HandledLines().Count == 1);
        _releaseSlowTap.SetResult();
        await WaitUntil(() => HandledLines().Count == 2);
        await host.StopAsync(Token);

        return (telegram, host.Services.GetRequiredService<ScreenVersions>());
    }

    private async Task<MusicArtistBase> SlowBusyAsync()
    {
        _slowTapStarted.SetResult();
        await _releaseSlowTap.Task;
        throw new MusicServiceUnavailableException("MusicBrainz", new HttpRequestException("503"));
    }
}
