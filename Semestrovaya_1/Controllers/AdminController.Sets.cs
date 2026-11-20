using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Npgsql;
using Semestrovaya_1.Models;
using Semestrovaya_1.Security;
using Semestrovaya_1.Templating;

namespace Semestrovaya_1.Controllers;

public sealed partial class AdminController
{
    private static readonly Regex SetCodePattern = new(
        @"\A[a-z0-9]+(?:-[a-z0-9]+)*\z",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    private static readonly Regex SetNamePattern = new(
        @"\A[^\p{Cc}]{1,150}\z",
        RegexOptions.Compiled,
        TimeSpan.FromSeconds(1));

    //один метод на три операции(создать/изменить/удалить)
    private async Task HandleSetsAsync(
        HttpListenerContext context,
        AdminSession session)
    {
        var request = context.Request;
        var path = request.Url?.AbsolutePath ?? "";

        if (path == "/admin/sets")
        {
            if (request.HttpMethod != "GET")
            {
                await MethodNotAllowedAsync(context, "GET");
                return;
            }

            await ShowSetsAsync(context);
            return;
        }

        var isCreate = path == "/admin/sets/create";
        var isEdit = path == "/admin/sets/edit";
        var isDelete = path == "/admin/sets/delete";

        if (!isCreate && !isEdit && !isDelete)
        {
            await SendAsync(
                context,
                404,
                "<h1>Раздел не найден</h1>"
                + "<a href=\"/admin/sets\">К наборам</a>");
            return;
        }

        if (request.HttpMethod != "GET"
            && request.HttpMethod != "POST")
        {
            await MethodNotAllowedAsync(context, "GET, POST");
            return;
        }

        // метод работает с entity одинаково для обеих
        //  операций: при создании это новый пустой объект,
        // при изменении/удалении — загруженный из базы.
        CardSet entity;//набор, с которым работаем

        if (isCreate)
        {
            entity = new CardSet();
        }
        else
        {
            if (!int.TryParse(request.QueryString["id"], out var id)
                || id <= 0)
            {
                await SendAsync(
                    context, 400, "Некорректный идентификатор набора.");
                return;
            }

            var existing = await _sets.GetByIdAsync(id);

            if (existing is null)
            {
                await SendAsync(
                    context,
                    404,
                    "<h1>Набор не найден</h1>"
                    + "<a href=\"/admin/sets\">К наборам</a>");
                return;
            }

            entity = existing;
        }

        if (request.HttpMethod == "GET")
        {
            if (isDelete)
            {
                await ShowSetDeleteAsync(
                    context, session, entity, "", 200);
            }
            else
            {
                await ShowSetFormAsync(
                    context,
                    session,
                    entity,
                    isCreate,
                    new Dictionary<string, string>(),
                    200);
            }

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
                "Сессия формы истекла. Открой страницу заново.");
            return;
        }

        if (isDelete)
        {
            await DeleteSetAsync(context, session, entity);
            return;
        }

        entity.Code = form.GetValueOrDefault("code", "").Trim();
        entity.Name = form.GetValueOrDefault("name", "").Trim();
        entity.Description =
            form.GetValueOrDefault("description", "").Trim();

        var errors = ValidateSet(entity);

        if (errors.Count > 0)
        {
            await ShowSetFormAsync(
                context, session, entity, isCreate, errors, 400);
            return;
        }

        try
        {
            if (isCreate)
            {
                await _sets.InsertAsync(entity);
            }
            else
            {
                var updated = await _sets.UpdateAsync(entity);

                if (!updated)
                {
                    await SendAsync(
                        context, 404, "Набор больше не существует.");
                    return;
                }
            }
        }
        catch (PostgresException exception)
            when (exception.SqlState == "23505")
        {
            errors["CodeError"] =
                "Набор с таким кодом уже существует.";

            await ShowSetFormAsync(
                context, session, entity, isCreate, errors, 409);
            return;
        }

        Redirect(context.Response, "/admin/sets");
    }

    private static Dictionary<string, string> ValidateSet(
        CardSet entity)
    {
        var errors = new Dictionary<string, string>();

        if (entity.Code.Length is < 1 or > 30
            || !SetCodePattern.IsMatch(entity.Code))
        {
            errors["CodeError"] =
                "Код: от 1 до 30 символов. "
                + "Используй маленькие латинские буквы, цифры "
                + "и одиночные дефисы между частями.";
        }

        if (!SetNamePattern.IsMatch(entity.Name))
        {
            errors["NameError"] =
                "Название: от 1 до 150 символов "
                + "без управляющих символов.";
        }

        if (entity.Description.Length > 2000)
        {
            errors["DescriptionError"] =
                "Описание не должно превышать 2000 символов.";
        }

        return errors;
    }

    private async Task ShowSetsAsync(
        HttpListenerContext context)
    {
        var sets = await _sets.GetAllAsync();

        var template = await File.ReadAllTextAsync(
            Path.Combine(_viewsDirectory, "Admin", "sets.html"));

        var model = new TemplateModel();

        model.Conditions["HasSets"] = sets.Count > 0;

        model.Collections["Sets"] = sets
            .Select(set =>
                (IReadOnlyDictionary<string, string>)
                new Dictionary<string, string>
                {
                    ["Id"] = set.Id.ToString(),
                    ["Code"] = set.Code,
                    ["Name"] = set.Name,
                    ["Description"] = set.Description
                })
            .ToList();

        var html = _templates.RenderPage(template, model);

        await SendAsync(context, 200, html);
    }

    private Task ShowSetFormAsync(
        HttpListenerContext context,
        AdminSession session,
        CardSet entity,
        bool isCreate,
        Dictionary<string, string> errors,
        int status)
    {
        return ShowTemplateAsync(
            context,
            "set-form.html",
            new Dictionary<string, string>
            {
                ["Title"] = isCreate
                    ? "Создание набора"
                    : "Редактирование набора",

                ["Action"] = isCreate
                    ? "/admin/sets/create"
                    : $"/admin/sets/edit?id={entity.Id}",

                ["CsrfToken"] = session.CsrfToken,
                ["Code"] = entity.Code,
                ["Name"] = entity.Name,
                ["Description"] = entity.Description,

                ["CodeError"] =
                    errors.GetValueOrDefault("CodeError", ""),

                ["NameError"] =
                    errors.GetValueOrDefault("NameError", ""),

                ["DescriptionError"] =
                    errors.GetValueOrDefault("DescriptionError", "")
            },
            status);
    }

    private Task ShowSetDeleteAsync(
        HttpListenerContext context,
        AdminSession session,
        CardSet entity,
        string error,
        int status)
    {
        return ShowTemplateAsync(
            context,
            "set-delete.html",
            new Dictionary<string, string>
            {
                ["Id"] = entity.Id.ToString(),
                ["Name"] = entity.Name,
                ["Code"] = entity.Code,
                ["CsrfToken"] = session.CsrfToken,
                ["Error"] = error
            },
            status);
    }

    private async Task DeleteSetAsync(
        HttpListenerContext context,
        AdminSession session,
        CardSet entity)
    {
        try
        {
            var deleted = await _sets.DeleteAsync(entity.Id);

            if (!deleted)
            {
                await SendAsync(
                    context, 404, "Набор больше не существует.");
                return;
            }
        }
        catch (PostgresException exception)
            when (exception.SqlState == "23503")
        {
            await ShowSetDeleteAsync(
                context,
                session,
                entity,
                "Нельзя удалить набор: с ним связаны карточки. "
                + "Сначала нужно изменить их принадлежность к набору.",
                409);
            return;
        }

        Redirect(context.Response, "/admin/sets");
    }
}