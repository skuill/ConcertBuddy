using Hqub.MusicBrainz.Entities;
using MusicSearcher.Converter;
using MusicSearcher.Model;
using SetlistNet.Models;
using SetlistNet.Models.ArrayResult;
using Shouldly;
using Artist = SetlistNet.Models.Artist;

namespace MusicSearcher.Tests.Converter;

public class SetlistConverterTest
{
    private const string Mbid = "65f4f0c5-ef9e-490c-aee3-909e7ae6b2ab";

    private static readonly Artist Metallica = new("Metallica", Mbid, "url", "Metallica", null, null);

    [Fact]
    public void ToInternal_Song_MapsAllFields()
    {
        // Arrange
        var song = new Song("Whiskey in the Jar", Metallica, new Artist("Thin Lizzy", "id", "url", "Thin Lizzy", null, null), "Acoustic", true);

        // Act
        var result = song.ToInternal();

        // Assert
        result.ShouldBeEquivalentTo(new MusicSong
        {
            Name = "Whiskey in the Jar",
            WithName = "Metallica",
            CoverName = "Thin Lizzy",
            Info = "Acoustic",
            Tape = true,
        });
    }

    [Fact]
    public void ToInternal_SongWithoutArtists_LeavesThemEmpty()
    {
        // Act
        var result = new Song("One", null, null, null, false).ToInternal();

        // Assert
        result.ShouldBeEquivalentTo(new MusicSong { Name = "One" });
    }

    [Fact]
    public void ToInternal_Setlist_MapsAllFields()
    {
        // Arrange
        var city = new City("1", "Stockholm", null!, new Country("Sweden", "SE"), "Stockholm County", "AB");
        var venue = new Venue("2", "Friends Arena", city, "url");
        var sets = new Sets(new[] { new Set(new[] { new Song("One", null, null, null, false) }, "Main", 1) });
        var setlist = new Setlist(Metallica, venue, new Tour("M72"), sets, "Rain", "https://www.setlist.fm/setlist/metallica/2026/x-4b48efd6.html", "4b48efd6", "v1", new DateTime(2026, 10, 31), new DateTime(2026, 11, 1));

        // Act
        var result = setlist.ToInternal();

        // Assert
        result.ShouldBeEquivalentTo(new MusicSetlist
        {
            Id = "4b48efd6",
            ArtistMBID = Mbid,
            ArtistName = "Metallica",
            VenueName = "Friends Arena",
            CityName = "Stockholm",
            CountryName = "Sweden",
            CountryState = "Stockholm County",
            EventDate = "31-10-2026",
            TourName = "M72",
            Info = "Rain",
            Url = "https://www.setlist.fm/setlist/metallica/2026/x-4b48efd6.html",
            Sets = new MusicSets
            {
                Set = new List<MusicSet>
                {
                    new() { Name = "Main", Encore = 1, Songs = new List<MusicSong> { new() { Name = "One" } } },
                },
            },
        });
    }

    [Fact]
    public void ToInternal_SetlistWithoutOptionalData_LeavesThemEmpty()
    {
        // Arrange
        var setlist = new Setlist(null!, null!, null, null!, null, null!, "4b48efd6", "v1", new DateTime(2026, 10, 31), new DateTime(2026, 11, 1));

        // Act
        var result = setlist.ToInternal();

        // Assert
        result.ShouldBeEquivalentTo(new MusicSetlist { Id = "4b48efd6", EventDate = "31-10-2026" });
    }

    [Fact]
    public void ToInternal_Setlists_MapsPagingAndItems()
    {
        // Arrange
        var setlist = new Setlist(null!, null!, null, null!, null, "url", "4b48efd6", "v1", new DateTime(2026, 10, 31), new DateTime(2026, 11, 1));
        var setlists = new Setlists(new[] { setlist }) { Page = 2, ItemsPerPage = 20, Total = 851 };

        // Act
        var result = setlists.ToInternal();

        // Assert
        result.ShouldBeEquivalentTo(new MusicSetlists
        {
            Page = 2,
            ItemsPerPage = 20,
            Total = 851,
            Setlist = new List<MusicSetlist> { new() { Id = "4b48efd6", EventDate = "31-10-2026", Url = "url" } },
        });
    }

    [Fact]
    public void ToInternal_Recording_MapsIdAndTitle()
    {
        // Arrange
        var recording = new Recording { Id = "a99d7f83", Title = "Wishing Wells" };

        // Act
        var result = recording.ToInternal();

        // Assert
        result.ShouldBeEquivalentTo(new MusicRecording { Id = "a99d7f83", Title = "Wishing Wells" });
    }
}
