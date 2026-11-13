using ConcertBuddy.ConsoleApp.Tests.Host;
using ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicBrainz;
using Shouldly;
using System.Net;
using System.Text.Json.Nodes;

namespace ConcertBuddy.ConsoleApp.Tests.Scenarios;

/// <summary>
/// Search → list → artist card in one message (#48), through the real host with a fake Telegram and fake music services.
/// </summary>
public class SearchScenarioTest : BotHostTestBase
{
    private const string Mbid = TestArtist.MetallicaMbid;

    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();

    [Fact]
    public async Task Search_ArtistsFound_SearchingThenTheListInTheSameMessage()
    {
        // Arrange
        ArtistsFor("Metallica", TestArtist.Metallica(), TestArtist.Numbered(2));
        var telegram = new FakeTelegram(CreateTextUpdate(1, "  Metallica "));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.Requests.Select(x => x.Method).ShouldBe(new[] { "sendMessage", "editMessageText" });
        var searching = telegram.RequestsOf("sendMessage").Single();
        searching["text"]!.GetValue<string>().ShouldBe("🔍 Searching for <b>Metallica</b>…");
        searching["parse_mode"]!.GetValue<string>().ShouldBe("HTML", StringCompareShould.IgnoreCase);
        searching["link_preview_options"]!["is_disabled"]!.GetValue<bool>().ShouldBeTrue();

        var list = telegram.RequestsOf("editMessageText").Single();
        list["message_id"]!.GetValue<int>().ShouldBe(FakeTelegram.SentMessageId);
        list["text"]!.GetValue<string>().ShouldBe("Choose the artist for <b>Metallica</b>:");
        ButtonData(list).ShouldBe(new[] { $"a|{Mbid}|0|Metallica", "a|00000000-0000-0000-0000-000000000002|0|Metallica", "/delete" });
        HandledLines().Single().ShouldStartWith("Handled update: Message /search in Private chat from user -: Found in ");
    }

    [Fact]
    public async Task Search_NothingFound_TipsAndExamplesAndNotFoundInTheLog()
    {
        // Arrange
        ArtistsFor("Metalica");
        var telegram = new FakeTelegram(CreateTextUpdate(1, "Metalica"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.RequestsOf("editMessageText").Single()["text"]!.GetValue<string>().ShouldStartWith("🤷 Nothing found for <b>Metalica</b>.");
        HandledLines().Single().ShouldContain(": NotFound in ");
    }

    [Fact]
    public async Task Search_MusicBrainzDownThenTryAgain_ListInTheSameMessage()
    {
        // Arrange: MusicBrainz fails once, then answers.
        A.CallTo(() => _music.SearchArtistsByName("Metallica", A<ScoreType>._, 5, 0))
            .ThrowsAsync(new MusicServiceUnavailableException("MusicBrainz", new HttpRequestException("503"))).Once()
            .Then.Returns(new MusicArtistBase[] { TestArtist.Metallica() });
        var telegram = new FakeTelegram(CreateTextUpdate(1, "Metallica"));
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        var unavailable = telegram.RequestsOf("editMessageText").Single();

        // Act
        telegram.AddUpdate(CreateCallbackUpdate(2, ButtonData(unavailable)[0]));
        await WaitUntil(() => HandledLines().Count == 2);
        await host.StopAsync(Token);

        // Assert
        unavailable["text"]!.GetValue<string>().ShouldStartWith("⚠️ MusicBrainz is busy or down right now.");
        HandledLines()[0].ShouldContain(": Unavailable in ");
        Logs.Entries.ShouldNotContain(x => x.Level >= LogLevel.Error);

        // The button is answered at once, then "Searching…" and the list replace the message.
        telegram.Requests.Skip(2).Select(x => x.Method).ShouldBe(new[] { "answerCallbackQuery", "editMessageText", "editMessageText" });
        var edits = telegram.RequestsOf("editMessageText");
        edits[1]["text"]!.GetValue<string>().ShouldBe("🔍 Searching for <b>Metallica</b>…");
        edits[2]["text"]!.GetValue<string>().ShouldBe("Choose the artist for <b>Metallica</b>:");
        edits[2]["message_id"]!.GetValue<int>().ShouldBe(FakeTelegram.SentMessageId);
        HandledLines()[1].ShouldStartWith("Handled update: CallbackQuery /search in Private chat from user -: Found in ");
    }

    [Fact]
    public async Task Artist_PickedFromTheList_CardInTheSameMessageAndResultsLeadBack()
    {
        // Arrange
        ArtistsFor("metallica", TestArtist.Metallica());
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).Returns(new MusicArtist(TestArtist.Metallica()));
        var telegram = new FakeTelegram(CreateTextUpdate(1, "metallica"));
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);

        // Act: pick the artist, then go back to the results.
        telegram.AddUpdate(CreateCallbackUpdate(2, ButtonData(telegram.RequestsOf("editMessageText").Last())[0]));
        await WaitUntil(() => HandledLines().Count == 2);
        var card = telegram.RequestsOf("editMessageText").Last();
        telegram.AddUpdate(CreateCallbackUpdate(3, ButtonData(card).Single(x => x.StartsWith("s|"))));
        await WaitUntil(() => HandledLines().Count == 3);
        await host.StopAsync(Token);

        // Assert
        card["text"]!.GetValue<string>().ShouldBe("🇺🇸 <b>Metallica</b>\nGroup · United States · 1981 -");
        ButtonData(card).ShouldContain("s|0|metallica");
        telegram.RequestsOf("answerCallbackQuery").Select(x => x["callback_query_id"]!.GetValue<string>()).ShouldBe(new[] { "q2", "q3" });
        telegram.RequestsOf("editMessageText").Last()["text"]!.GetValue<string>().ShouldBe("Choose the artist for <b>metallica</b>:");
        telegram.RequestsOf("sendMessage").Count.ShouldBe(1);
        HandledLines()[1].ShouldStartWith("Handled update: CallbackQuery /artist in Private chat from user -: Found in ");
    }

    [Fact]
    public async Task Search_TextLongerThanAButtonHolds_ButtonsStillWork()
    {
        // Arrange
        var text = "The Very Long Name Of A Band That Does Not Fit Into The Button";
        ArtistsFor(text, TestArtist.Metallica());
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).Returns(new MusicArtist(TestArtist.Metallica()));
        var telegram = new FakeTelegram(CreateTextUpdate(1, text));
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        var pick = ButtonData(telegram.RequestsOf("editMessageText").Single())[0];

        // Act
        telegram.AddUpdate(CreateCallbackUpdate(2, pick));
        await WaitUntil(() => HandledLines().Count == 2);
        await host.StopAsync(Token);

        // Assert
        pick.ShouldStartWith("~");
        HandledLines()[1].ShouldContain(": Found in ");
        ButtonData(telegram.RequestsOf("editMessageText").Last()).ShouldContain(x => x.StartsWith("~"));
    }

    [Theory]
    [InlineData("~12345")]
    [InlineData($"/artist {Mbid}")]
    [InlineData("/search 5 5 Metallica")]
    [InlineData("x|whatever")]
    [InlineData("s|not-a-number|Metallica")]
    [InlineData("a|not-a-guid|0|Metallica")]
    [InlineData("a|only-mbid")]
    [InlineData(null)]
    public async Task Button_UnknownOrOfAnOlderRun_OutdatedAlertAndNoEdit(string? data)
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, data));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        var answer = telegram.Requests.Single();
        answer.Method.ShouldBe("answerCallbackQuery");
        answer.Body["text"]!.GetValue<string>().ShouldBe("This button is outdated. Send the artist name again.");
        answer.Body["show_alert"]!.GetValue<bool>().ShouldBeTrue();
        HandledLines().Single().ShouldContain(": Outdated in ");
    }

    [Fact]
    public async Task Button_SameScreenAgain_NotModifiedIsNotAnError()
    {
        // Arrange: a second tap on a page that is already shown.
        ArtistsFor("Metallica", TestArtist.Metallica());
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, "s|5|Metallica"))
        {
            EditResponse = () => FakeTelegram.Json(HttpStatusCode.BadRequest, """{"ok":false,"error_code":400,"description":"Bad Request: message is not modified: specified new message content and reply markup are exactly the same"}"""),
        };
        A.CallTo(() => _music.SearchArtistsByName("Metallica", A<ScoreType>._, 5, 5)).Returns(new MusicArtistBase[] { TestArtist.Numbered(6) });

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        HandledLines().Single().ShouldContain(": Found in ");
        telegram.RequestsOf("sendMessage").ShouldBeEmpty();
    }

    [Fact]
    public async Task Button_ScreenFailsWithABug_ErrorAndTheButtonAnsweredOnce()
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).ThrowsAsync(new InvalidOperationException("bug"));
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"a|{Mbid}|0|"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        HandledLines().Single().ShouldContain(": Error in ");
        telegram.RequestsOf("answerCallbackQuery").Count.ShouldBe(1);
        telegram.Answers.Single().ShouldContain("Something goes wrong");
    }

    [Fact]
    public async Task Button_OldCommandFails_TheHandlerAnswersTheButton()
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistSetlists(Mbid, A<int>._)).ThrowsAsync(new InvalidOperationException("bug"));
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"/setlists 1 1 {Mbid}"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        HandledLines().Single().ShouldContain(": Error in ");
        telegram.RequestsOf("answerCallbackQuery").Single()["callback_query_id"]!.GetValue<string>().ShouldBe("q1");
    }

    private void ArtistsFor(string text, params MusicArtistBase[] artists)
    {
        A.CallTo(() => _music.SearchArtistsByName(text, A<ScoreType>._, 5, 0)).Returns(artists);
    }

    private async Task RunUntilHandled(FakeTelegram telegram, int updates)
    {
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == updates);
        await host.StopAsync(Token);
    }

    private static List<string> ButtonData(JsonNode request)
    {
        return request["reply_markup"]!["inline_keyboard"]!.AsArray()
            .SelectMany(row => row!.AsArray())
            .Select(button => button!["callback_data"]!.GetValue<string>())
            .ToList();
    }
}
