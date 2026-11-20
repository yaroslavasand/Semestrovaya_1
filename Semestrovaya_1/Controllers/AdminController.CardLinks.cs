using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Npgsql;
using Semestrovaya_1.Models;
using Semestrovaya_1.Orm;
using Semestrovaya_1.Repositories;
using Semestrovaya_1.Security;
using Semestrovaya_1.Templating;

namespace Semestrovaya_1.Controllers;

public sealed partial class AdminController
{
    //принимает запрос, всё проверяет, сохраняет
    private async Task HandleCardLinksAsync(
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

        if (!int.TryParse(request.QueryString["id"], out var cardId)
            || cardId <= 0)
        {
            await SendAsync(
                context, 400, "Некорректный идентификатор карточки.");
            return;
        }

        var cards = new OrmRepository<CardEdit>(_adminDatabase);
        var card = await cards.GetByIdAsync(cardId);

        if (card is null)
        {
            await SendAsync(context, 404, "Карточка не найдена.");
            return;
        }

        var links = new CardLinksRepository(_adminDatabase);
        var options = await links.GetOptionsAsync(cardId);//список всех возможных галочек для страницы: все домены, все типы, все теги из справочников

        //просто показ страницы
        if (request.HttpMethod == "GET")
        {
            await ShowCardLinksAsync(
                context, session, card, options, "", 200);
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

        var allowedNames = options
            .Select(option => $"{option.Group}_{option.Id}")
            .ToHashSet(StringComparer.Ordinal);

        foreach (var field in form)
        {
            if (field.Key == "csrf")
            {
                continue;
            }

            if (!allowedNames.Contains(field.Key)
                || field.Value != "1")
            {
                await SendAsync(
                    context,
                    400,
                    "В форме указаны неизвестные значения. "
                    + "Открой страницу заново.");
                return;
            }
        }

        var selectedOptions = options
            .Select(option => option with
            {
                Selected = form.ContainsKey(
                    $"{option.Group}_{option.Id}")
            })
            .ToList();

        //массив id-шников выбранных элементов из группы X
        int[] SelectedIds(string group)
        {
            return selectedOptions
                .Where(option =>
                    option.Group == group && option.Selected)
                .Select(option => option.Id)
                .ToArray();
        }

        try
        {
            var saved = await links.SaveAsync(
                cardId,
                SelectedIds("domains"),
                SelectedIds("types"),
                SelectedIds("tags"));

            if (!saved)
            {
                await SendAsync(
                    context, 404, "Карточка больше не существует.");
                return;
            }
        }
        
        //обработка гонки по справочникам
        catch (PostgresException exception)
            when (exception.SqlState == "23503")
        {
            var freshOptions = await links.GetOptionsAsync(cardId);

            var refreshedSelection = freshOptions
                .Select(option => option with
                {
                    Selected = form.ContainsKey(
                        $"{option.Group}_{option.Id}")
                })
                .ToList();

            await ShowCardLinksAsync(
                context,
                session,
                card,
                refreshedSelection,
                "Один из выбранных элементов был удалён. "
                + "Предыдущие связи не изменились. "
                + "Проверь выбор и сохрани снова.",
                409);

            return;
        }

        Redirect(context.Response, "/admin/cards?saved=1");
    }

    //метод берёт шаблон card-links.html,
    //подставляет в него общие значения (имя карточки, токен, ошибку),
    //а потом для каждой из трёх групп генерирует HTML-куски
    //с галочками и вставляет их в страницу
    private async Task ShowCardLinksAsync(
        HttpListenerContext context,
        AdminSession session,
        CardEdit card,
        List<CardLinkOption> options,
        string error,
        int status)
    {
        var template = await File.ReadAllTextAsync(
            Path.Combine(
                _viewsDirectory, "Admin", "card-links.html"));

        var model = new TemplateModel();

        model.Values["Id"] = card.Id.ToString();
        model.Values["Name"] = card.Name;
        model.Values["CsrfToken"] = session.CsrfToken;
        model.Values["Error"] = error;

        void AddGroup(
            string group,
            string conditionName,
            string collectionName)
        {
            var items = options
                .Where(option => option.Group == group)
                .Select(option =>
                    (IReadOnlyDictionary<string, string>)
                    new Dictionary<string, string>
                    {
                        ["FieldName"] =
                            $"{option.Group}_{option.Id}",
                        ["Name"] = option.Name,
                        ["Checked"] =
                            option.Selected ? "checked" : ""
                    })
                .ToList();

            model.Conditions[conditionName] = items.Count > 0;
            model.Collections[collectionName] = items;
        }

        AddGroup("domains", "HasDomains", "DomainOptions");
        AddGroup("types", "HasTypes", "TypeOptions");
        AddGroup("tags", "HasTags", "TagOptions");

        var page = _templates.RenderPage(template, model);

        await SendAsync(context, status, page);
    }
}