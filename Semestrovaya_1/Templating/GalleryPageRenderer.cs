using Semestrovaya_1.Repositories;

namespace Semestrovaya_1.Templating;

//сборщик главной страницы
//берет index.html, находит в нём специальный комментарий-метку,
//вклеивает туда шаблон, берёт карточки из базы и прогоняет
//всё через шаблонизатор
public sealed class GalleryPageRenderer
{
    private const string CardsMarker = "<!-- gallery:cards -->";

    private readonly CardRepository _repository;
    private readonly TemplateEngine _templateEngine;
    private readonly string _viewsDirectory;

    public GalleryPageRenderer(
        CardRepository repository,
        TemplateEngine templateEngine,
        string viewsDirectory)
    {
        _repository = repository;
        _templateEngine = templateEngine;
        _viewsDirectory = viewsDirectory;
    }


    public async Task<string> RenderAsync(
        string? search = null,
        string? sort = null,
        string? set = null,
        bool newOnly = false)
    {
        var pagePath = Path.Combine(
            _viewsDirectory,
            "index.html");

        var cardPath = Path.Combine(
            _viewsDirectory,
            "Partials",
            "card.html");

        var pageTemplate = await File.ReadAllTextAsync(pagePath);
        var cardTemplate = await File.ReadAllTextAsync(cardPath);

        if (!pageTemplate.Contains(CardsMarker, StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "В index.html не найден маркер <!-- gallery:cards -->.");
        }

        // Включаем частичный шаблон карточек в страницу
        // до рендеринга: дальше всё обрабатывает шаблонизатор.
        var template = pageTemplate.Replace(
            CardsMarker,
            cardTemplate,
            StringComparison.Ordinal);

        var cards = await _repository.GetAllAsync(
            search,
            sort,
            set,
            newOnly);

        var model = new TemplateModel();

        model.Conditions["HasCards"] = cards.Count > 0;

        model.Collections["Cards"] = cards
            .Select(card =>
                (IReadOnlyDictionary<string, string>)
                new Dictionary<string, string>
                {
                    ["Code"] = card.Code,
                    ["Name"] = card.Name,
                    ["ImagePath"] = card.ImagePath
                })
            .ToList();

        return _templateEngine.RenderPage(template, model);
    }
}