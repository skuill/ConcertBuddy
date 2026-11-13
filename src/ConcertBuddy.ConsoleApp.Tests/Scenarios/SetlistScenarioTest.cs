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
/// Artist card → setlists → setlist → back (#48), in one message that turns rich and plain again.
/// </summary>
public class SetlistScenarioTest : BotHostTestBase
{
    private const string Mbid = TestArtist.MetallicaMbid;
    private const string SetlistId = "4b48efd6";

    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();

    public SetlistScenarioTest()
    {
        var setlist = new MusicSetlist
        {
            Id = SetlistId,
            EventDate = "31-10-2001",
            ArtistName = "Metallica",
            CityName = "Stockholm",
            VenueName = "Friends Arena",
            Sets = new MusicSets { Set = new List<MusicSet> { new() { Songs = new List<MusicSong> { new() { Name = "One" } } } } },
        };
        A.CallTo(() => _music.SearchArtistSetlists(Mbid, 1)).Returns(new MusicSetlists { Setlist = new List<MusicSetlist> { setlist }, Total = 1 });
        A.CallTo(() => _music.SearchSetlist(SetlistId)).Returns(setlist);
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).Returns(new MusicArtist(TestArtist.Metallica()));
    }

    [Fact]
    public async Task Setlists_ToSetlistAndBackToTheArtist_SameMessageRichThenPlain()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"l|{Mbid}|1"));
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        var setlists = telegram.RequestsOf("editMessageText").Single();

        // Act: open the setlist, back to the setlists, back to the artist.
        telegram.AddUpdate(CreateCallbackUpdate(2, ButtonData(setlists)[0]));
        await WaitUntil(() => HandledLines().Count == 2);
        var setlist = telegram.RequestsOf("editMessageText").Last();
        telegram.AddUpdate(CreateCallbackUpdate(3, ButtonData(setlist).Last()));
        await WaitUntil(() => HandledLines().Count == 3);
        telegram.AddUpdate(CreateCallbackUpdate(4, ButtonData(setlists).Last()));
        await WaitUntil(() => HandledLines().Count == 4);
        await host.StopAsync(Token);

        // Assert
        setlists["text"]!.GetValue<string>().ShouldStartWith("📝 <b>Metallica</b> · 1 setlists");
        setlist["text"].ShouldBeNull();
        setlist["rich_message"]!["html"]!.GetValue<string>().ShouldStartWith("<h3>Metallica</h3>");
        setlist["message_id"]!.GetValue<int>().ShouldBe(FakeTelegram.SentMessageId);
        var edits = telegram.RequestsOf("editMessageText");
        edits[2]["text"]!.GetValue<string>().ShouldStartWith("📝 <b>Metallica</b>");
        edits[3]["text"]!.GetValue<string>().ShouldStartWith("🇺🇸 <b>Metallica</b>");
        edits[3]["rich_message"].ShouldBeNull();
        HandledLines().Select(x => x.Split(" in Private")[0]).ShouldBe(new[]
        {
            "Handled update: CallbackQuery /setlists",
            "Handled update: CallbackQuery /setlist",
            "Handled update: CallbackQuery /setlists",
            "Handled update: CallbackQuery /artist",
        });
        telegram.RequestsOf("answerCallbackQuery").Count.ShouldBe(4);
    }

    [Fact]
    public async Task Biography_FromTheCard_RichInTheSameMessage()
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).Returns(new MusicArtist(new TestArtist { NameValue = "Metallica", FullBiographyValue = "Formed in 1981." }));
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"b|{Mbid}"));

        // Act
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        await host.StopAsync(Token);

        // Assert
        telegram.RequestsOf("editMessageText").Single()["rich_message"]!["html"]!.GetValue<string>().ShouldBe("<h3>📖 Metallica</h3><p>Formed in 1981.</p>");
        HandledLines().Single().ShouldStartWith("Handled update: CallbackQuery /biography in Private chat from user -: Found in ");
    }

    [Theory]
    [InlineData("l|not-a-guid|1")]
    [InlineData($"l|{Mbid}|0")]
    [InlineData($"l|{Mbid}")]
    [InlineData($"t||{Mbid}|1")]
    [InlineData($"t|{SetlistId}|{Mbid}|0")]
    [InlineData($"t|{SetlistId}|x|1")]
    [InlineData("b|not-a-guid")]
    public async Task Button_BadArguments_Outdated(string data)
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, data));

        // Act
        using var host = CreateHost(telegram, services => services.AddScoped(_ => _music));
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        await host.StopAsync(Token);

        // Assert
        HandledLines().Single().ShouldContain(": Outdated in ");
        telegram.RequestsOf("editMessageText").ShouldBeEmpty();
    }

    private static List<string> ButtonData(JsonNode request)
    {
        return request["reply_markup"]!["inline_keyboard"]!.AsArray()
            .SelectMany(row => row!.AsArray())
            .Select(button => button!["callback_data"]?.GetValue<string>())
            .OfType<string>()
            .ToList();
    }
}
