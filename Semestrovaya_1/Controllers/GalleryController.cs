using System.Collections.Specialized;
using Semestrovaya_1.Models;
using Semestrovaya_1.Templating;

namespace Semestrovaya_1.Controllers;

//читает параметры из адресной строки и проверяет их по «белым спискам»
public sealed class GalleryController
{
    private static readonly HashSet<string> AllowedSorts = new(
        StringComparer.Ordinal)
    {
        "gallery",
        "card-asc",
        "card-desc",
        "name-asc",
        "name-desc",
        "might-asc",
        "might-desc",
        "rarity-asc",
        "rarity-desc"
    };

    private static readonly HashSet<string> AllowedSets = new(
        StringComparer.Ordinal)
    {
        "radiance",
        "vendetta",
        "unleashed",
        "spiritforged",
        "origins-main",
        "origins-proving-grounds",
        "all"
    };

    private readonly GalleryPageRenderer _renderer;

    public GalleryController(GalleryPageRenderer renderer)
    {
        _renderer = renderer;
    }

    public Task<string> IndexAsync(NameValueCollection query)
    {
        var options = ReadQuery(query);

        return _renderer.RenderAsync(
            options.Search,
            options.Sort,
            options.Set,
            options.NewOnly);
    }

    private static GalleryQuery ReadQuery(NameValueCollection query)
    {
        var requestedSort = query["sort"] ?? "";
        var requestedSet = query["set"] ?? "";

        return new GalleryQuery
        {
            Search = (query["q"] ?? "").Trim(),

            Sort = AllowedSorts.Contains(requestedSort)
                ? requestedSort
                : "gallery",

            Set = AllowedSets.Contains(requestedSet)
                ? requestedSet
                : "radiance",

            NewOnly = string.Equals(
                query["new"],
                "true",
                StringComparison.OrdinalIgnoreCase)
        };
    }
}