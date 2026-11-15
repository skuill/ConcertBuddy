using ConcertBuddy.ConsoleApp.Tests.Host;
using ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using MusicSearcher;
using MusicSearcher.Model;
using Shouldly;
using System.Text.Json.Nodes;

namespace ConcertBuddy.ConsoleApp.Tests.Scenarios;

/// <summary>
/// Sharing a setlist (#48): "Send to a chat" through inline mode, the share link and opening it.
/// </summary>
public class ShareScenarioTest : BotHostTestBase
{
    private const string Mbid = TestArtist.MetallicaMbid;
    private const string SetlistId = "4b48efd6";

    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();

    public ShareScenarioTest()
    {
        var setlist = new MusicSetlist
        {
            Id = SetlistId,
            EventDate = "31-10-2001",
            ArtistMBID = Mbid,
            ArtistName = "Metallica",
            CityName = "Stockholm",
            VenueName = "Friends Arena",
            Url = "https://www.setlist.fm/setlist/x.html",
            Sets = new MusicSets { Set = new List<MusicSet> { new() { Songs = new List<MusicSong> { new() { Name = "One" } } } } },
        };
        A.CallTo(() => _music.SearchSetlist(SetlistId)).Returns(setlist);
    }

    [Fact]
    public async Task InlineQuery_SharedSetlist_OneCardWithTextSongsAndAnOpenButton()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateInlineUpdate(1, $"s_{SetlistId}"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        var answer = telegram.RequestsOf("answerInlineQuery").Single();
        answer["inline_query_id"]!.GetValue<string>().ShouldBe("iq1");
        answer["cache_time"]!.GetValue<int>().ShouldBe(300);
        var result = answer["results"]!.AsArray().Single()!;
        result["type"]!.GetValue<string>().ShouldBe("article");
        result["id"]!.GetValue<string>().ShouldBe(SetlistId);
        result["title"]!.GetValue<string>().ShouldBe("Metallica · 31 Oct 2001");
        result["description"]!.GetValue<string>().ShouldBe("Friends Arena, Stockholm");
        var html = result["input_message_content"]!["rich_message"]!["html"]!.GetValue<string>();
        html.ShouldContain("<tr><td>1</td><td>One</td></tr>");
        html.ShouldNotContain("tg-button");
        html.ShouldContain("<footer>Setlist from <a href=\"https://www.setlist.fm/setlist/x.html\">setlist.fm</a></footer>");
        var open = result["reply_markup"]!["inline_keyboard"]![0]![0]!;
        open["text"]!.GetValue<string>().ShouldBe("🎸 Open in ConcertBuddy");
        open["url"]!.GetValue<string>().ShouldBe($"https://t.me/test_bot?start=s_{SetlistId}");
        HandledLines().Single().ShouldStartWith("Handled update: InlineQuery /share in Group chat from user -: Found in ");
    }

    [Theory]
    [InlineData("")]
    [InlineData("metallica")]
    [InlineData("s_")]
    [InlineData("s_not-hex!")]
    public async Task InlineQuery_NotASharedSetlist_NoResults(string query)
    {
        // Arrange
        var telegram = new FakeTelegram(CreateInlineUpdate(1, query));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.RequestsOf("answerInlineQuery").Single()["results"]!.AsArray().ShouldBeEmpty();
        HandledLines().Single().ShouldContain(": NotFound in ");
        A.CallTo(() => _music.SearchSetlist(A<string>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task InlineQuery_SetlistFmDown_NoResultsNotCached()
    {
        // Arrange
        A.CallTo(() => _music.SearchSetlist(SetlistId)).ThrowsAsync(new MusicServiceUnavailableException("setlist.fm", new HttpRequestException("500")));
        var telegram = new FakeTelegram(CreateInlineUpdate(1, $"s_{SetlistId}"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        var answer = telegram.RequestsOf("answerInlineQuery").Single();
        answer["results"]!.AsArray().ShouldBeEmpty();
        answer["cache_time"]!.GetValue<int>().ShouldBe(0);
        HandledLines().Single().ShouldContain(": Unavailable in ");
    }

    [Fact]
    public async Task StartLink_SharedSetlist_OpeningThenTheSetlistWithButtons()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateTextUpdate(1, $"/start s_{SetlistId}"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.RequestsOf("sendMessage").Single()["text"]!.GetValue<string>().ShouldBe("📝 Opening the shared setlist…");
        var edit = telegram.RequestsOf("editMessageText").Single();
        edit["rich_message"]!["html"]!.GetValue<string>().ShouldContain($"data=\"p|{Mbid}|One\"");
        var buttons = edit["reply_markup"]!["inline_keyboard"]!.AsArray().SelectMany(row => row!.AsArray()).ToList();
        buttons.Select(button => button!["text"]!.GetValue<string>()).ShouldBe(new[] { "📋 Copy setlist", "↗️ Send to a chat", "🔗 Share link", "⬅️ Setlists" });
        buttons[1]!["switch_inline_query"]!.GetValue<string>().ShouldBe($"s_{SetlistId}");
        buttons[2]!["url"]!.GetValue<string>().ShouldStartWith("https://t.me/share/url?url=https%3A%2F%2Ft.me%2Ftest_bot%3Fstart%3Ds_4b48efd6&text=");
        buttons[3]!["callback_data"]!.GetValue<string>().ShouldBe($"l|{Mbid}|1");
        HandledLines().Single().ShouldStartWith("Handled update: Message /share in Private chat from user -: Found in ");
    }

    [Fact]
    public async Task StartLink_SetlistGone_SaysSoWithMenu()
    {
        // Arrange
        A.CallTo(() => _music.SearchSetlist(SetlistId)).Returns((MusicSetlist?)null);
        var telegram = new FakeTelegram(CreateTextUpdate(1, $"/start s_{SetlistId}"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        var edit = telegram.RequestsOf("editMessageText").Single();
        edit["text"]!.GetValue<string>().ShouldBe("🤷 setlist.fm does not have this setlist any more.");
        edit["reply_markup"]!["inline_keyboard"]![0]![0]!["callback_data"]!.GetValue<string>().ShouldBe("m");
        HandledLines().Single().ShouldContain(": NotFound in ");
    }

    [Fact]
    public async Task StartLink_SetlistFmDownThenTryAgain_TheSetlistInTheSameMessage()
    {
        // Arrange
        A.CallTo(() => _music.SearchSetlist(SetlistId))
            .ThrowsAsync(new MusicServiceUnavailableException("setlist.fm", new HttpRequestException("500"))).Once()
            .Then.Returns(new MusicSetlist { Id = SetlistId, EventDate = "31-10-2001", ArtistMBID = Mbid, ArtistName = "Metallica" });
        var telegram = new FakeTelegram(CreateTextUpdate(1, $"/start s_{SetlistId}"));
        using var host = CreateTestHost(telegram);
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        var busy = telegram.RequestsOf("editMessageText").Single();

        // Act
        var tryAgain = busy["reply_markup"]!["inline_keyboard"]![0]![0]!["callback_data"]!.GetValue<string>();
        telegram.AddUpdate(CreateCallbackUpdate(2, tryAgain));
        await WaitUntil(() => HandledLines().Count == 2);
        await host.StopAsync(Token);

        // Assert
        tryAgain.ShouldBe($"k|{SetlistId}");
        telegram.RequestsOf("editMessageText").Last()["rich_message"]!["html"]!.GetValue<string>().ShouldStartWith("<h3>Metallica</h3>");
        HandledLines()[1].ShouldStartWith("Handled update: CallbackQuery /share in Private chat from user -: Found in ");
    }

    [Theory]
    [InlineData("/start s_")]
    [InlineData("/start s_zz-not-a-setlist")]
    [InlineData("/start hello")]
    public async Task StartLink_NotASetlist_TheStartMenu(string text)
    {
        // Arrange
        var telegram = new FakeTelegram(CreateTextUpdate(1, text));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.RequestsOf("sendMessage").Single()["text"]!.GetValue<string>().ShouldStartWith("👋 Hi, Tester!");
        A.CallTo(() => _music.SearchSetlist(A<string>._)).MustNotHaveHappened();
    }

    [Theory]
    [InlineData("k|not-hex!")]
    [InlineData("k|")]
    public async Task TryAgain_BadSetlistId_Outdated(string data)
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, data));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        HandledLines().Single().ShouldContain(": Outdated in ");
    }

    [Fact]
    public async Task Polling_AsksForInlineQueriesExplicitly()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateTextUpdate(1, "/help"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.LastPollBody!["allowed_updates"]!.AsArray().Select(x => x!.GetValue<string>())
            .ShouldBe(new[] { "message", "edited_message", "callback_query", "inline_query" });
    }

    private Microsoft.Extensions.Hosting.IHost CreateTestHost(FakeTelegram telegram)
    {
        return CreateHost(telegram, services => services.AddScoped(_ => _music));
    }

    private async Task RunUntilHandled(FakeTelegram telegram, int updates)
    {
        using var host = CreateTestHost(telegram);
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == updates);
        await host.StopAsync(Token);
    }

    private static string CreateInlineUpdate(int updateId, string query)
    {
        var inline = new JsonObject
        {
            ["id"] = $"iq{updateId}",
            ["from"] = new JsonObject { ["id"] = TesterId, ["is_bot"] = false, ["first_name"] = "Tester" },
            ["query"] = query,
            ["offset"] = string.Empty,
            ["chat_type"] = "group",
        };
        return new JsonObject { ["update_id"] = updateId, ["inline_query"] = inline }.ToJsonString();
    }
}
