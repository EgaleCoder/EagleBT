namespace AudioHub.Core.Models;

/// <summary>
/// Encapsulates metadata for an active audio or media playback stream.
/// </summary>
public sealed record MediaMetadata
{
    public string Title { get; init; } = string.Empty;
    public string Artist { get; init; } = string.Empty;
    public string AlbumTitle { get; init; } = string.Empty;
    public string SourceAppId { get; init; } = string.Empty;
    public int TrackNumber { get; init; }

    public bool HasMetadata => !string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(Artist);

    public string DisplayText
    {
        get
        {
            if (string.IsNullOrWhiteSpace(Title) && string.IsNullOrWhiteSpace(Artist))
            {
                return "No Media Playing";
            }

            if (!string.IsNullOrWhiteSpace(Artist) && !string.IsNullOrWhiteSpace(Title))
            {
                return $"{Artist} — {Title}";
            }

            return !string.IsNullOrWhiteSpace(Title) ? Title : Artist;
        }
    }
}
