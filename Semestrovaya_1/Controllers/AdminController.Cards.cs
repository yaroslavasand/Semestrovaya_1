using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Semestrovaya_1.Models;
using Semestrovaya_1.Orm;
using Semestrovaya_1.Security;
using Semestrovaya_1.Templating;

namespace Semestrovaya_1.Controllers;

public sealed partial class AdminController
{
    private static readonly Regex CardNamePattern = new(
        @"\A[^\p{Cc}]{1,200}\z",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private async Task HandleCardsAsync(
        HttpListenerContext context,
        AdminSession session)
    {
        var request = context.Request;
        var path = request.Url?.AbsolutePath ?? "";
        
        if (path == "/admin/cards/create")
        {
            await HandleCardCreateAsync(context, session);
            return;
        }

        if (path == "/admin/cards/delete")
        {
            await HandleCardDeleteAsync(context, session);
            return;
        }
        
        if (path == "/admin/cards/links")
        {
            await HandleCardLinksAsync(context, session);
            return;
        }

        if (path == "/admin/cards")
        {
            if (request.HttpMethod != "GET")
            {
                await MethodNotAllowedAsync(context, "GET");
                return;
            }

            await ShowAdminCardsAsync(context);
            return;
        }

        if (path != "/admin/cards/edit")
        {
            await SendAsync(context, 404, "Раздел не найден.");
            return;
        }

        if (request.HttpMethod != "GET"
            && request.HttpMethod != "POST")
        {
            await MethodNotAllowedAsync(context, "GET, POST");
            return;
        }

        if (!int.TryParse(request.QueryString["id"], out var id)
            || id <= 0)
        {
            await SendAsync(
                context, 400, "Некорректный идентификатор карточки.");
            return;
        }

        var repository = new OrmRepository<CardEdit>(_adminDatabase);
        var card = await repository.GetByIdAsync(id);

        if (card is null)
        {
            await SendAsync(
                context,
                404,
                "<h1>Карточка не найдена</h1>"
                + "<a href=\"/admin/cards\">К списку карточек</a>");
            return;
        }

        var values = CreateCardFormValues(card, session);

        if (request.HttpMethod == "GET")
        {
            await ShowTemplateAsync(
                context, "card-form.html", values, 200);
            return;
        }

        var form = await ReadFormAsync(request);

        if (form is null)
        {
            await SendAsync(context, 400, "Некорректная форма.");
            return;
        }

        if (!ValidCsrf(session, form))
        {
            await SendAsync(
                context,
                403,
                "Сессия формы истекла. Открой карточку заново.");
            return;
        }

        var name = form.GetValueOrDefault("name", "").Trim();
        var artist = form.GetValueOrDefault("artist", "").Trim();
        var ability = form.GetValueOrDefault("ability", "").Trim();

        var energyText = form.GetValueOrDefault("energy", "").Trim();
        var powerText = form.GetValueOrDefault("power", "").Trim();
        var mightText = form.GetValueOrDefault("might", "").Trim();

        var isNew = form.GetValueOrDefault("isNew", "") == "true";

        values["Name"] = name;
        values["Artist"] = artist;
        values["Ability"] = ability;
        values["Energy"] = energyText;
        values["Power"] = powerText;
        values["Might"] = mightText;
        values["IsNewChecked"] = isNew ? "checked" : "";

        var errors = new List<string>();

        if (!CardNamePattern.IsMatch(name))
        {
            errors.Add(
                "Название: от 1 до 200 символов "
                + "без управляющих символов.");
        }

        if (artist.Length > 200
            || artist.Any(char.IsControl))
        {
            errors.Add(
                "Имя автора: не более 200 символов "
                + "без управляющих символов.");
        }

        if (ability.Length > 8000)
        {
            errors.Add(
                "Текст способности: не более 8000 символов.");
        }

        var energy = ParseCardNumber(
            energyText, "Energy", errors);

        var power = ParseCardNumber(
            powerText, "Power", errors);

        var might = ParseCardNumber(
            mightText, "Might", errors);

        if (errors.Count > 0)
        {
            values["Error"] = string.Join(" ", errors);

            await ShowTemplateAsync(
                context, "card-form.html", values, 400);
            return;
        }

        card.Name = name;
        card.Artist = artist;
        card.Ability = ability;
        card.Energy = energy;
        card.Power = power;
        card.Might = might;
        card.IsNew = isNew;
        card.UpdatedAt = DateTime.UtcNow;

        var updated = await repository.UpdateAsync(card);

        if (!updated)
        {
            await SendAsync(
                context, 404, "Карточка больше не существует.");
            return;
        }

        Redirect(context.Response, "/admin/cards?saved=1");
    }

    private async Task ShowAdminCardsAsync(
        HttpListenerContext context)
    {
        const string sql = """
                           SELECT id, code, name, image_path
                           FROM public.cards
                           ORDER BY display_order, code;
                           """;

        var cards = await _adminDatabase.QueryAsync(
            sql,
            reader => new Dictionary<string, string>
            {
                ["Id"] = reader.GetInt32(0).ToString(
                    CultureInfo.InvariantCulture),

                ["Code"] = reader.GetString(1),
                ["Name"] = reader.GetString(2),
                ["ImagePath"] = reader.GetString(3)
            });

        var template = await File.ReadAllTextAsync(
            Path.Combine(
                _viewsDirectory, "Admin", "cards.html"));

        var model = new TemplateModel();

        model.Values["Count"] = cards.Count.ToString(
            CultureInfo.InvariantCulture);

        model.Values["Message"] =
            context.Request.QueryString["saved"] == "1"
                ? "Изменения карточки сохранены."
                : context.Request.QueryString["deleted"] == "1"
                    ? "Карточка удалена."
                    : "";

        model.Collections["Cards"] = cards
            .Select(card =>
                (IReadOnlyDictionary<string, string>)card)
            .ToList();

        var page = _templates.RenderPage(template, model);

        await SendAsync(context, 200, page);
    }

    private static Dictionary<string, string> CreateCardFormValues(
        CardEdit card,
        AdminSession session)
    {
        return new Dictionary<string, string>
        {
            ["Id"] = card.Id.ToString(CultureInfo.InvariantCulture),
            ["CsrfToken"] = session.CsrfToken,
            ["Name"] = card.Name,
            ["Artist"] = card.Artist,
            ["Ability"] = card.Ability,

            ["Energy"] = card.Energy?.ToString(
                CultureInfo.InvariantCulture) ?? "",

            ["Power"] = card.Power?.ToString(
                CultureInfo.InvariantCulture) ?? "",

            ["Might"] = card.Might?.ToString(
                CultureInfo.InvariantCulture) ?? "",

            ["IsNewChecked"] = card.IsNew ? "checked" : "",
            ["Error"] = ""
        };
    }

    private static int? ParseCardNumber(
        string text,
        string field,
        List<string> errors)
    {
        if (text.Length == 0)
        {
            return null;
        }

        if (!int.TryParse(
                text,
                NumberStyles.None,
                CultureInfo.InvariantCulture,
                out var value)
            || value < 0)
        {
            errors.Add(
                $"{field}: введи целое неотрицательное число "
                + "или оставь поле пустым.");

            return null;
        }

        return value;
    }
}