namespace Semestrovaya_1.Models;

public sealed class GalleryQuery
{
    public string Search { get; init; } = "";

    public string Sort { get; init; } = "gallery";

    public string Set { get; init; } = "radiance";

    public bool NewOnly { get; init; }
}