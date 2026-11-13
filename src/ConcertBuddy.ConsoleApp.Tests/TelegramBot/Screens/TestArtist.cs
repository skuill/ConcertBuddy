using MusicSearcher.Model.Abstract;
using MusicSearcher.MusicService;

namespace ConcertBuddy.ConsoleApp.Tests.TelegramBot.Screens;

/// <summary>
/// An artist of one music service with the values the test sets.
/// </summary>
internal sealed class TestArtist : MusicArtistBase
{
    public const string MetallicaMbid = "65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab";

    public MusicServiceType Service { get; init; } = MusicServiceType.MusicBrainz;

    public override MusicServiceType MusicServiceType => Service;

    public string? NameValue { get; init; }

    public override string? Name => NameValue;

    public string? MbidValue { get; init; }

    public override string? MBID => MbidValue;

    public override int? Score => null;

    public override Uri? ImageUri => null;

    public string? BiographyValue { get; init; }

    public override string? Biography => BiographyValue;

    public string? AreaValue { get; init; }

    public override string? Area => AreaValue;

    public string? ActiveYearsValue { get; init; }

    public override string? ActiveYears => ActiveYearsValue;

    public string? TypeValue { get; init; }

    public override string? Type => TypeValue;

    public string? CountryValue { get; init; }

    public override string? Country => CountryValue;

    public Uri? ExternalUrlValue { get; init; }

    public override Uri? ExternalUrl => ExternalUrlValue;

    public static TestArtist Metallica() => new()
    {
        NameValue = "Metallica",
        MbidValue = MetallicaMbid,
        TypeValue = "Group",
        AreaValue = "United States",
        CountryValue = "US",
        ActiveYearsValue = "1981 - ",
    };

    /// <summary>
    /// Artist number <paramref name="number"/> of a search result, each with its own MBID.
    /// </summary>
    public static TestArtist Numbered(int number) => new()
    {
        NameValue = $"Artist {number}",
        MbidValue = $"00000000-0000-0000-0000-{number:D12}",
        TypeValue = "Person",
    };
}
