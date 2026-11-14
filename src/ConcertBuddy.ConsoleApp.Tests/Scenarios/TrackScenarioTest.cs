using ConcertBuddy.ConsoleApp.Tests.Host;
using ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;
using FakeItEasy;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using MusicSearcher;
using MusicSearcher.Model;
using MusicSearcher.Model.Abstract;
using Shouldly;
using System.Net;
using System.Text.Json;
using System.Text.Json.Nodes;
using Telegram.Bot;
using Telegram.Bot.Types;

namespace ConcertBuddy.ConsoleApp.Tests.Scenarios;

/// <summary>
/// A song of a setlist: a new track message with the audio, the lyrics shown and hidden in place (#48).
/// </summary>
public class TrackScenarioTest : BotHostTestBase
{
    private const string Mbid = TestArtist.MetallicaMbid;
    private const string AudioUrl = "https://audio.example/one.mp3";
    private static readonly byte[] AudioBytes = new byte[] { 1, 2, 3, 4, 5 };

    private readonly IMusicSearcherClient _music = A.Fake<IMusicSearcherClient>();
    private HttpStatusCode _audioStatus = HttpStatusCode.OK;

    public TrackScenarioTest()
    {
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).Returns(new MusicArtist(TestArtist.Metallica()));
        A.CallTo(() => _music.SearchTrack("Metallica", "One")).Returns(Track(AudioUrl));
    }

    [Fact]
    public async Task Play_AudioFound_AnswersAtOnceAndSendsOneRichMessageWithTheUpload()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"p|{Mbid}|One"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert: the setlist message is not touched, the track is a new message.
        telegram.Requests.Select(x => x.Method).ShouldBe(new[] { "answerCallbackQuery", "sendChatAction", "sendRichMessage" });
        telegram.RequestsOf("answerCallbackQuery").Single()["text"]!.GetValue<string>().ShouldBe("⏳ Loading One…");
        telegram.RequestsOf("sendChatAction").Single()["action"]!.GetValue<string>().ShouldBe("upload_document");

        var sent = telegram.RequestsOf("sendRichMessage").Single();
        var rich = sent["rich_message"]!;
        rich["html"]!.GetValue<string>().ShouldBe("<figure><audio src=\"tg://audio?id=track\"></audio><figcaption>Metallica – One</figcaption></figure>");
        var media = rich["media"]!.AsArray().Single()!;
        media["id"]!.GetValue<string>().ShouldBe("track");
        media["media"]!["type"]!.GetValue<string>().ShouldBe("audio");
        media["media"]!["title"]!.GetValue<string>().ShouldBe("One");
        media["media"]!["performer"]!.GetValue<string>().ShouldBe("Metallica");
        var file = sent.AsObject().Select(field => field.Value).OfType<JsonObject>().Single(value => value.ContainsKey("file_name"));
        file["file_name"]!.GetValue<string>().ShouldBe("Metallica - One.mp3");
        file["size"]!.GetValue<int>().ShouldBe(AudioBytes.Length);
        HandledLines().Single().ShouldStartWith("Handled update: CallbackQuery /track in Private chat from user -: Found in ");
    }

    [Fact]
    public async Task Play_NoAudioLink_TrackWithoutPlayer()
    {
        // Arrange
        A.CallTo(() => _music.SearchTrack("Metallica", "One")).Returns(Track(null));
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"p|{Mbid}|One"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        var rich = telegram.RequestsOf("sendRichMessage").Single()["rich_message"]!;
        rich["html"]!.GetValue<string>().ShouldBe("<h3>Metallica – One</h3><p>🔇 No audio for this song.</p>");
        rich["media"].ShouldBeNull();
        HandledLines().Single().ShouldContain(": NotFound in ");
    }

    [Fact]
    public async Task Play_DownloadFails_TrackWithoutPlayerAndAWarning()
    {
        // Arrange
        _audioStatus = HttpStatusCode.NotFound;
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"p|{Mbid}|One"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.RequestsOf("sendRichMessage").Single()["rich_message"]!["html"]!.GetValue<string>().ShouldContain("🔇 No audio");
        Logs.Entries.ShouldContain(x => x.Level == LogLevel.Warning && x.Message == "Can't download the audio of a track, sending it without audio");
    }

    [Fact]
    public async Task Play_MusicBrainzDownThenTryAgain_TrackAndTheBusyMessageDeleted()
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistByMBID(Mbid))
            .ThrowsAsync(new MusicServiceUnavailableException("MusicBrainz", new HttpRequestException("503"))).Once()
            .Then.Returns(new MusicArtist(TestArtist.Metallica()));
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"p|{Mbid}|One"));
        using var host = CreateTestHost(telegram);
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == 1);
        var busy = telegram.RequestsOf("sendMessage").Single();

        // Act: Try again under the busy message.
        var tryAgain = busy["reply_markup"]!["inline_keyboard"]![0]![0]!["callback_data"]!.GetValue<string>();
        telegram.AddUpdate(CreateCallbackUpdate(2, tryAgain));
        await WaitUntil(() => HandledLines().Count == 2);
        await host.StopAsync(Token);

        // Assert
        busy["text"]!.GetValue<string>().ShouldStartWith("⚠️ MusicBrainz is busy or down right now.");
        tryAgain.ShouldBe($"q|{Mbid}|One");
        HandledLines()[0].ShouldContain(": Unavailable in ");
        telegram.RequestsOf("sendRichMessage").Count.ShouldBe(1);
        telegram.RequestsOf("deleteMessage").Single()["message_id"]!.GetValue<int>().ShouldBe(FakeTelegram.SentMessageId);
        HandledLines()[1].ShouldContain(": Found in ");
    }

    [Fact]
    public async Task Play_ArtistGoneFromMusicBrainz_SaysSoWithoutSearchingTheTrack()
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).Returns(new MusicArtist());
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"p|{Mbid}|One"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.RequestsOf("sendMessage").Single()["text"]!.GetValue<string>().ShouldBe("🤷 MusicBrainz does not have this artist any more.");
        telegram.RequestsOf("sendRichMessage").ShouldBeEmpty();
        A.CallTo(() => _music.SearchTrack(A<string>._, A<string>._)).MustNotHaveHappened();
        HandledLines().Single().ShouldContain(": NotFound in ");
    }

    [Fact]
    public async Task ShowLyrics_MessageWithAudio_SameMessageKeepsTheAudioAndAddsTheLyrics()
    {
        // Arrange: the artist name comes from the audio of the message, MusicBrainz is not asked for it.
        A.CallTo(() => _music.SearchLyric("Metallica", "One")).Returns(new MusicLyric { IsSuccessSearchResult = true, LyricText = "I can't remember anything" });
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"y|{Mbid}|One", richMessage: TrackMessage()));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.RequestsOf("answerCallbackQuery").Single()["text"]!.GetValue<string>().ShouldBe("⏳ Looking for the lyrics…");
        var edit = telegram.RequestsOf("editMessageText").Single();
        edit["message_id"]!.GetValue<int>().ShouldBe(FakeTelegram.SentMessageId);
        var rich = edit["rich_message"]!;
        rich["html"]!.GetValue<string>().ShouldBe("<figure><audio src=\"tg://audio?id=track\"></audio><figcaption>Metallica – One</figcaption></figure><hr/><p>I can't remember anything</p>");
        rich["media"]![0]!["media"]!["media"]!.GetValue<string>().ShouldBe("file-1");
        edit["reply_markup"]!["inline_keyboard"]![0]![0]!["text"]!.GetValue<string>().ShouldBe("🎼 Hide lyrics");
        A.CallTo(() => _music.SearchArtistByMBID(A<string>._)).MustNotHaveHappened();
        HandledLines().Single().ShouldStartWith("Handled update: CallbackQuery /lyric in Private chat from user -: Found in ");
    }

    [Fact]
    public async Task HideLyrics_MessageWithAudio_PlayerOnlyWithoutASearch()
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"h|{Mbid}|One", richMessage: TrackMessage()));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.RequestsOf("answerCallbackQuery").Single()["text"].ShouldBeNull();
        var edit = telegram.RequestsOf("editMessageText").Single();
        edit["rich_message"]!["html"]!.GetValue<string>().ShouldBe("<figure><audio src=\"tg://audio?id=track\"></audio><figcaption>Metallica – One</figcaption></figure>");
        edit["reply_markup"]!["inline_keyboard"]![0]![0]!["text"]!.GetValue<string>().ShouldBe("🎼 Lyrics");
        A.CallTo(() => _music.SearchLyric(A<string>._, A<string>._)).MustNotHaveHappened();
    }

    [Fact]
    public async Task ShowLyrics_NoAudioAndMusicBrainzDown_SaysBusyInTheMessage()
    {
        // Arrange
        A.CallTo(() => _music.SearchArtistByMBID(Mbid)).ThrowsAsync(new MusicServiceUnavailableException("MusicBrainz", new TimeoutException()));
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"y|{Mbid}|One", richMessage: new JsonObject { ["blocks"] = new JsonArray() }));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        telegram.RequestsOf("editMessageText").Single()["rich_message"]!["html"]!.GetValue<string>()
            .ShouldBe("<h3>One</h3><p>🔇 No audio for this song.</p><p>⚠️ MusicBrainz is busy right now, tap Lyrics again in a minute.</p>");
        HandledLines().Single().ShouldContain(": Unavailable in ");
    }

    [Fact]
    public async Task TopTracks_FromTheCard_ListInTheSameMessage()
    {
        // Arrange
        var top = A.Fake<MusicTrackBase>();
        A.CallTo(() => top.TrackName).Returns("Enter Sandman");
        A.CallTo(() => _music.SearchTopTracks(Mbid)).Returns(new[] { top });
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, $"o|{Mbid}"));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        var edit = telegram.RequestsOf("editMessageText").Single();
        edit["text"]!.GetValue<string>().ShouldStartWith("📻 <b>Top tracks</b>");
        edit["reply_markup"]!["inline_keyboard"]![0]![0]!["callback_data"]!.GetValue<string>().ShouldBe($"p|{Mbid}|Enter Sandman");
        HandledLines().Single().ShouldStartWith("Handled update: CallbackQuery /top in Private chat from user -: Found in ");
    }

    [Theory]
    [InlineData("p|not-a-guid|One")]
    [InlineData($"p|{Mbid}|")]
    [InlineData($"q|{Mbid}")]
    [InlineData("y|not-a-guid|One")]
    [InlineData($"h|{Mbid}|")]
    [InlineData("o|not-a-guid")]
    public async Task Button_BadArguments_Outdated(string data)
    {
        // Arrange
        var telegram = new FakeTelegram(CreateCallbackUpdate(1, data));

        // Act
        await RunUntilHandled(telegram, 1);

        // Assert
        HandledLines().Single().ShouldContain(": Outdated in ");
        telegram.Requests.Select(x => x.Method).ShouldBe(new[] { "answerCallbackQuery" });
    }

    private Microsoft.Extensions.Hosting.IHost CreateTestHost(FakeTelegram telegram)
    {
        return CreateHost(telegram, services =>
        {
            services.AddScoped(_ => _music);
            services.AddHttpClient(ServiceCollectionExtensions.AudioClientName).ConfigurePrimaryHttpMessageHandler(() => new AudioHandler(this));
        });
    }

    private async Task RunUntilHandled(FakeTelegram telegram, int updates)
    {
        using var host = CreateTestHost(telegram);
        await host.StartAsync(Token);
        await WaitUntil(() => HandledLines().Count == updates);
        await host.StopAsync(Token);
    }

    /// <summary>
    /// The rich message of a track as Telegram sends it back: an audio block with the uploaded file.
    /// </summary>
    private static JsonObject TrackMessage()
    {
        var rich = new RichMessage
        {
            Blocks = new RichBlock[] { new RichBlockAudio { Audio = new Audio { FileId = "file-1", FileUniqueId = "u-1", Duration = 446, Performer = "Metallica", Title = "One" } } },
        };
        return JsonSerializer.SerializeToNode(rich, JsonBotAPI.Options)!.AsObject();
    }

    private static MusicTrackBase Track(string? downloadLink)
    {
        var track = A.Fake<MusicTrackBase>();
        A.CallTo(() => track.TrackName).Returns("One");
        A.CallTo(() => track.DownloadLink).Returns(downloadLink);
        return track;
    }

    private sealed class AudioHandler(TrackScenarioTest test) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.RequestUri!.ToString().ShouldBe(AudioUrl);
            return Task.FromResult(new HttpResponseMessage(test._audioStatus) { Content = new ByteArrayContent(AudioBytes) });
        }
    }
}
