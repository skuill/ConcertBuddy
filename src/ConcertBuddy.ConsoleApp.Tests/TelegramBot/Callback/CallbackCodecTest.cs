using ConcertBuddy.ConsoleApp.TelegramBot.Callback;
using Shouldly;
using System.Text;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Callback;

public class CallbackCodecTest
{
    private const string Mbid = "65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab";

    private readonly CallbackCodec _codec = new();

    [Fact]
    public void Encode_ShortPayload_GoesIntoTheButtonAndDecodes()
    {
        // Act
        var data = _codec.Encode("a", Mbid, "0", "Metallica");
        var decoded = _codec.TryDecode(data, out var payload);

        // Assert
        data.ShouldBe($"a|{Mbid}|0|Metallica");
        decoded.ShouldBeTrue();
        payload.ShouldBe(new CallbackPayload("a", $"{Mbid}|0|Metallica"));
        payload.Split(3).ShouldBe(new[] { Mbid, "0", "Metallica" });
    }

    [Fact]
    public void Encode_Exactly64Bytes_StaysInTheButton()
    {
        // Arrange: "s|0|" is 4 bytes.
        var text = new string('x', CallbackCodec.MaxBytes - 4);

        // Act
        var data = _codec.Encode("s", "0", text);

        // Assert
        data.ShouldBe($"s|0|{text}");
    }

    [Fact]
    public void Encode_65Bytes_StoredAndDecodedToTheSamePayload()
    {
        // Arrange
        var text = new string('x', CallbackCodec.MaxBytes - 3);

        // Act
        var data = _codec.Encode("s", "0", text);
        var decoded = _codec.TryDecode(data, out var payload);

        // Assert
        data.ShouldStartWith("~");
        Encoding.UTF8.GetByteCount(data).ShouldBeLessThanOrEqualTo(CallbackCodec.MaxBytes);
        decoded.ShouldBeTrue();
        payload.ShouldBe(new CallbackPayload("s", $"0|{text}"));
    }

    [Fact]
    public void Encode_CyrillicText_CountsBytesNotCharacters()
    {
        // Arrange: 31 characters, 62 bytes in UTF-8, 66 with "s|0|".
        var text = new string('ж', 31);

        // Act
        var data = _codec.Encode("s", "0", text);

        // Assert
        data.ShouldStartWith("~");
        _codec.TryDecode(data, out var payload).ShouldBeTrue();
        payload.Split(2).ShouldBe(new[] { "0", text });
    }

    [Fact]
    public void Encode_TwoLongPayloads_TwoIds()
    {
        // Act
        var first = _codec.Encode("s", "0", new string('x', 70));
        var second = _codec.Encode("s", "0", new string('y', 70));

        // Assert
        first.ShouldNotBe(second);
        _codec.TryDecode(second, out var payload).ShouldBeTrue();
        payload.Arguments.ShouldBe($"0|{new string('y', 70)}");
    }

    [Fact]
    public void TryDecode_StoredIdOfAnotherRun_False()
    {
        // Arrange: the bot restarted, the memory is new.
        var data = new CallbackCodec().Encode("s", "0", new string('x', 70));

        // Act
        var decoded = _codec.TryDecode(data, out _);

        // Assert
        decoded.ShouldBeFalse();
    }

    [Fact]
    public void Split_SeparatorInTheLastArgument_KeptInIt()
    {
        // Arrange
        var data = _codec.Encode("s", "5", "AC|DC");

        // Act
        _codec.TryDecode(data, out var payload);

        // Assert
        payload.Split(2).ShouldBe(new[] { "5", "AC|DC" });
    }

    [Theory]
    [InlineData("s", 2)]
    [InlineData("s|0", 2)]
    [InlineData("a|mbid|0", 3)]
    public void Split_FewerArguments_Null(string data, int count)
    {
        // Act
        _codec.TryDecode(data, out var payload);

        // Assert
        payload.Split(count).ShouldBeNull();
    }
}
