namespace Semestrovaya_1.Templating;

// Модель данных для единого рендеринга страницы:
// простые значения, условия и коллекции в одном месте.
public sealed class TemplateModel
{
    public Dictionary<string, string> Values { get; } =
        new(StringComparer.Ordinal);

    public Dictionary<string, bool> Conditions { get; } =
        new(StringComparer.Ordinal);

    public Dictionary<
            string,
            IReadOnlyList<IReadOnlyDictionary<string, string>>>
        Collections { get; } = new(StringComparer.Ordinal);
}