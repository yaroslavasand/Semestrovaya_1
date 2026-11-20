using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Npgsql;
using Semestrovaya_1.Models;
using Semestrovaya_1.Orm;
using Semestrovaya_1.Security;
using Semestrovaya_1.Templating;

namespace Semestrovaya_1.Controllers;

public sealed partial class AdminController
{
    private async Task HandleCardCreateAsync(
        HttpListenerContext context,
        AdminSession session)
    {
        var request = context.Request;

        if (request.HttpMethod != "GET"
            && request.HttpMethod != "POST")
        {
            await MethodNotAllowedAsync(context, "GET, POST");
            return;
        }

        var sets = await new OrmRepository<CardSet>(
            _adminDatabase).GetAllAsync();

        var images = GetAvailableCardImages();
        var values = new Dictionary<string, string>
        {
            ["CsrfToken"] = session.CsrfToken,
            ["Code"] = "",
            ["Name"] = "",
            ["Error"] = ""
        };

        var selectedSet = "";
        var selectedImage = "";

        if (request.HttpMethod == "POST")
        {
            var form = await ReadFormAsync(request);

            if (form is null || !ValidCsrf(session, form))
            {
                await SendAsync(
                    context,
                    403,
                    "Некорректная форма или истёкшая сессия.");
                return;
            }

            var code = form.GetValueOrDefault("code", "").Trim();
            var name = form.GetValueOrDefault("name", "").Trim();

            selectedSet = form.GetValueOrDefault("setId", "");
            selectedImage = form.GetValueOrDefault("imagePath", "");

            values["Code"] = code;
            values["Name"] = name;

            var errors = new List<string>();

            if (code.Length is < 1 or > 50
                || !SetCodePattern.IsMatch(code))
            {
                errors.Add(
                    "Код: 1–50 символов, маленькие латинские "
                    + "буквы, цифры и одиночные дефисы между частями.");
            }

            if (!CardNamePattern.IsMatch(name))
            {
                errors.Add(
                    "Название: 1–200 символов "
                    + "без управляющих символов.");
            }

            var validSet =
                int.TryParse(selectedSet, out var setId)
                && sets.Any(set => set.Id == setId);

            if (!validSet)
            {
                errors.Add("Выбери существующий набор.");
            }

            if (!images.Contains(
                    selectedImage,
                    StringComparer.Ordinal))
            {
                errors.Add("Выбери изображение из списка.");
            }

            if (errors.Count == 0)
            {
                var repository =
                    new OrmRepository<CardRecord>(_adminDatabase);

                var card = new CardRecord
                {
                    Code = code,
                    Name = name,
                    CardSetId = setId,
                    ImagePath = selectedImage,
                    DisplayOrder = 0
                };

                try
                {
                    var newId = await repository.InsertAsync(card);

                    Redirect(
                        context.Response,
                        $"/admin/cards/edit?id={newId}");

                    return;
                }
                catch (PostgresException exception)
                    when (exception.SqlState == "23505")
                {
                    errors.Add(
                        "Карточка с таким кодом уже существует.");
                }
                catch (PostgresException exception)
                    when (exception.SqlState == "23503")
                {
                    errors.Add(
                        "Выбранный набор больше не существует. "
                        + "Обнови страницу и выбери другой.");
                }
            }

            values["Error"] = string.Join(" ", errors);
        }

        await ShowCardCreateAsync(
            context,
            values,
            sets,
            images,
            selectedSet,
            selectedImage,
            request.HttpMethod == "POST" ? 400 : 200);
    }

    private static List<string> GetAvailableCardImages()
    {
        var directory = Path.Combine(
            AppContext.BaseDirectory,
            "Static",
            "images",
            "cards");

        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }

        var extensions = new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            ".webp", ".png", ".jpg", ".jpeg"
        };

        return Directory.EnumerateFiles(directory)
            .Where(file => extensions.Contains(
                Path.GetExtension(file)))
            .Select(file =>
                "/Static/images/cards/" + Path.GetFileName(file))
            .OrderBy(path => path, StringComparer.Ordinal)
            .ToList();
    }

    private async Task ShowCardCreateAsync(
        HttpListenerContext context,
        Dictionary<string, string> values,
        List<CardSet> sets,
        List<string> images,
        string selectedSet,
        string selectedImage,
        int status)
    {
        var template = await File.ReadAllTextAsync(
            Path.Combine(
                _viewsDirectory, "Admin", "card-create.html"));

        var model = new TemplateModel();

        foreach (var pair in values)
        {
            model.Values[pair.Key] = pair.Value;
        }

        model.Collections["SetOptions"] = sets
            .Select(set =>
                (IReadOnlyDictionary<string, string>)
                new Dictionary<string, string>
                {
                    ["Value"] = set.Id.ToString(),
                    ["Text"] = $"{set.Name} ({set.Code})",
                    ["Selected"] =
                        set.Id.ToString() == selectedSet
                            ? "selected"
                            : ""
                })
            .ToList();

        model.Collections["ImageOptions"] = images
            .Select(image =>
                (IReadOnlyDictionary<string, string>)
                new Dictionary<string, string>
                {
                    ["Value"] = image,
                    ["Text"] = Path.GetFileName(image),
                    ["Selected"] =
                        image == selectedImage ? "selected" : ""
                })
            .ToList();

        var page = _templates.RenderPage(template, model);

        await SendAsync(context, status, page);
    }

    private async Task HandleCardDeleteAsync(
        HttpListenerContext context,
        AdminSession session)
    {
        var request = context.Request;

        if (request.HttpMethod != "GET"
            && request.HttpMethod != "POST")
        {
            await MethodNotAllowedAsync(context, "GET, POST");
            return;
        }

        if (!int.TryParse(request.QueryString["id"], out var id)
            || id <= 0)
        {
            await SendAsync(context, 400, "Некорректный Id карточки.");
            return;
        }

        var repository =
            new OrmRepository<CardRecord>(_adminDatabase);

        var card = await repository.GetByIdAsync(id);

        if (card is null)
        {
            await SendAsync(context, 404, "Карточка не найдена.");
            return;
        }

        if (request.HttpMethod == "GET")
        {
            await ShowTemplateAsync(
                context,
                "card-delete.html",
                new Dictionary<string, string>
                {
                    ["Id"] = card.Id.ToString(),
                    ["Name"] = card.Name,
                    ["Code"] = card.Code,
                    ["CsrfToken"] = session.CsrfToken
                },
                200);

            return;
        }

        var form = await ReadFormAsync(request);

        if (form is null || !ValidCsrf(session, form))
        {
            await SendAsync(
                context,
                403,
                "Некорректная форма или истёкшая сессия.");
            return;
        }

        if (!await repository.DeleteAsync(id))
        {
            await SendAsync(
                context, 404, "Карточка уже удалена.");
            return;
        }

        Redirect(context.Response, "/admin/cards?deleted=1");
    }
}