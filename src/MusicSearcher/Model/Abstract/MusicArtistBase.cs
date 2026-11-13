using MusicSearcher.MusicService;

namespace MusicSearcher.Model.Abstract;

public abstract class MusicArtistBase : IMusicArtist
{
    public virtual MusicServiceType MusicServiceType
    {
        get => throw new NotImplementedException();
    }
    public abstract string? Name { get; }
    public abstract string? MBID { get; }
    public abstract int? Score { get; }
    public abstract Uri? ImageUri { get; }
    public abstract string? Biography { get; }

    /// <summary>
    /// The whole biography; only Last.fm has it, <see cref="Biography"/> is its summary.
    /// </summary>
    public virtual string? FullBiography => null;
    /// <summary>
    /// Areas are geographic regions or settlements.
    /// </summary>
    public abstract string? Area { get; }
    public abstract string? ActiveYears { get; }
    public abstract string? Type { get; }
    public abstract string? Country { get; }
    public abstract Uri? ExternalUrl { get; }

    public virtual MusicArtistBase? GetMusicArtistByServiceType(MusicServiceType musicServiceType)
        => MusicServiceType == musicServiceType ? this : null;

    public virtual bool IsMusicArtistExist(MusicServiceType musicServiceType)
        => MusicServiceType == musicServiceType;
}
