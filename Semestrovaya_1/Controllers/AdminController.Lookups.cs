using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Npgsql;
using Semestrovaya_1.Orm;
using Semestrovaya_1.Security;
using Semestrovaya_1.Templating;

namespace Semestrovaya_1.Controllers;

public sealed partial class AdminController
{
    //// Один обобщённый обработчик на три раздела-справочника
    // (домены, типы, теги): у них одинаковые CRUD-операции,
    // различаются только тип сущности, адрес, заголовок
    // и лимит длины кода
    private async Task HandleLookupAsync<T>(
        HttpListenerContext context,
        AdminSession session,
        string basePath,
        string title,
        int codeMax)
        where T : class, new()
    {
        var request = context.Request;
        var path = request.Url?.AbsolutePath ?? "";

        var isList = path == basePath;
        var isCreate = path == basePath + "/create";
        var isEdit = path == basePath + "/edit";
        var isDelete = path == basePath + "/delete";

        if (!isList && !isCreate && !isEdit && !isDelete)
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

        var repository = new OrmRepository<T>(_adminDatabase);
        var map = EntityMapCache.Get<T>();

        var codeProperty = map.Columns.Single(
            column => column.Name == "code").Property;

        var nameProperty = map.Columns.Single(
            column => column.Name == "name").Property;

        var descriptionProperty = map.Columns.Single(
            column => column.Name == "description").Property;

        string ReadText(T item, PropertyInfo property)
        {
            return (string?)property.GetValue(item) ?? "";
        }

        int ReadId(T item)
        {
            return (int)map.PrimaryKey.Property.GetValue(item)!;
        }

        async Task ShowFormAsync(
            T item,
            Dictionary<string, string> errors,
            int status)
        {
            await ShowTemplateAsync(
                context,
                "lookup-form.html",
                new Dictionary<string, string>
                {
                    ["Title"] = title,
                    ["Heading"] = isCreate
                        ? "Создание записи"
                        : "Редактирование записи",

                    ["BasePath"] = basePath,
                    ["Action"] = isCreate
                        ? basePath + "/create"
                        : $"{basePath}/edit?id={ReadId(item)}",

                    ["CsrfToken"] = session.CsrfToken,
                    ["Code"] = ReadText(item, codeProperty),
                    ["Name"] = ReadText(item, nameProperty),
                    ["Description"] = ReadText(item, descriptionProperty),
                    ["CodeMax"] = codeMax.ToString(),

                    ["CodeError"] =
                        errors.GetValueOrDefault("CodeError", ""),

                    ["NameError"] =
                        errors.GetValueOrDefault("NameError", ""),

                    ["DescriptionError"] =
                        errors.GetValueOrDefault("DescriptionError", "")
                },
                status);
        }

        async Task ShowDeleteAsync(
            T item,
            string error,
            int status)
        {
            await ShowTemplateAsync(
                context,
                "lookup-delete.html",
                new Dictionary<string, string>
                {
                    ["Title"] = title,
                    ["BasePath"] = basePath,
                    ["Id"] = ReadId(item).ToString(),
                    ["Name"] = ReadText(item, nameProperty),
                    ["Code"] = ReadText(item, codeProperty),
                    ["CsrfToken"] = session.CsrfToken,
                    ["Error"] = error
                },
                status);
        }

        if (isList)
        {
            if (request.HttpMethod != "GET")
            {
                await MethodNotAllowedAsync(context, "GET");
                return;
            }

            var records = await repository.GetAllAsync();

            var template = await File.ReadAllTextAsync(
                Path.Combine(
                    _viewsDirectory, "Admin", "lookup-list.html"));

            var model = new TemplateModel();

            model.Values["Title"] = title;
            model.Values["BasePath"] = basePath;
            model.Values["Count"] = records.Count.ToString();

            model.Collections["Items"] = records.Select(item =>
                (IReadOnlyDictionary<string, string>)
                new Dictionary<string, string>
                {
                    ["Id"] = ReadId(item).ToString(),
                    ["Code"] = ReadText(item, codeProperty),
                    ["Name"] = ReadText(item, nameProperty),
                    ["Description"] =
                        ReadText(item, descriptionProperty),
                    ["BasePath"] = basePath
                }).ToList();

            var page = _templates.RenderPage(template, model);

            await SendAsync(context, 200, page);

            return;
        }

        T entity;

        if (isCreate)
        {
            entity = new T();
        }
        else
        {
            if (!int.TryParse(request.QueryString["id"], out var id)
                || id <= 0)
            {
                await SendAsync(context, 400, "Некорректный Id.");
                return;
            }

            var existing = await repository.GetByIdAsync(id);

            if (existing is null)
            {
                await SendAsync(context, 404, "Запись не найдена.");
                return;
            }

            entity = existing;
        }

        if (request.HttpMethod == "GET")
        {
            if (isDelete)
            {
                await ShowDeleteAsync(entity, "", 200);
            }
            else
            {
                await ShowFormAsync(
                    entity, new Dictionary<string, string>(), 200);
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
            try
            {
                if (!await repository.DeleteAsync(ReadId(entity)))
                {
                    await SendAsync(
                        context, 404, "Запись больше не существует.");
                    return;
                }
            }
            catch (PostgresException exception)
                when (exception.SqlState == "23503")
            {
                await ShowDeleteAsync(
                    entity,
                    "Нельзя удалить запись: она используется карточками.",
                    409);

                return;
            }

            Redirect(context.Response, basePath);
            return;
        }

        var code = form.GetValueOrDefault("code", "").Trim();
        var name = form.GetValueOrDefault("name", "").Trim();
        var description =
            form.GetValueOrDefault("description", "").Trim();

        codeProperty.SetValue(entity, code);
        nameProperty.SetValue(entity, name);
        descriptionProperty.SetValue(entity, description);

        var errors = new Dictionary<string, string>();

        if (code.Length is < 1
            || code.Length > codeMax
            || !SetCodePattern.IsMatch(code))
        {
            errors["CodeError"] =
                $"Код: 1–{codeMax} символов. "
                + "Маленькие латинские буквы, цифры "
                + "и одиночные дефисы между частями.";
        }

        if (name.Length is < 1 or > 100
            || !SetNamePattern.IsMatch(name))
        {
            errors["NameError"] =
                "Название: 1–100 символов без управляющих символов.";
        }

        if (description.Length > 2000)
        {
            errors["DescriptionError"] =
                "Описание не должно превышать 2000 символов.";
        }

        if (errors.Count > 0)
        {
            await ShowFormAsync(entity, errors, 400);
            return;
        }

        try
        {
            if (isCreate)
            {
                await repository.InsertAsync(entity);
            }
            else if (!await repository.UpdateAsync(entity))
            {
                await SendAsync(
                    context, 404, "Запись больше не существует.");
                return;
            }
        }
        catch (PostgresException exception)
            when (exception.SqlState == "23505")
        {
            errors["CodeError"] =
                "Запись с таким кодом уже существует.";

            await ShowFormAsync(entity, errors, 409);
            return;
        }

        Redirect(context.Response, basePath);
    }
}