using Semestrovaya_1.Repositories;
using Semestrovaya_1.Templating;

namespace Semestrovaya_1.Controllers;
//собирает про карточку всё,  вытягивает из связанных таблиц и склеивает
//в строки через запятую. Результат рендерится в шаблон card-details.html
public sealed class CardController
{
    private const string DetailsCardCode = "rad-sp5-005";

    private readonly CardRepository _repository;
    private readonly TemplateEngine _templateEngine;
    private readonly string _viewsDirectory;

    public CardController(
        CardRepository repository,
        TemplateEngine templateEngine,
        string viewsDirectory)
    {
        _repository = repository;
        _templateEngine = templateEngine;
        _viewsDirectory = viewsDirectory;
    }

    public async Task<string?> SeraphineAsync()
    {
        var values = await _repository.GetDetailsAsync(
            DetailsCardCode);

        if (values is null)
        {
            return null;
        }
        var displayCode = values["Code"].ToUpperInvariant();
        var lastSeparator = displayCode.LastIndexOf('-');

        if (lastSeparator >= 0)
        {
            displayCode =
                displayCode[..lastSeparator]
                + "/"
                + displayCode[(lastSeparator + 1)..];
        }

        values["DisplayCode"] = displayCode;

        
        if (values["Artist"] == "—")
        {
            values["Artist"] = "";
        }

        var templatePath = Path.Combine(
            _viewsDirectory,
            "Partials",
            "card-details.html");

        var template = await File.ReadAllTextAsync(templatePath);

        return _templateEngine.Render(template, values);
    }
}