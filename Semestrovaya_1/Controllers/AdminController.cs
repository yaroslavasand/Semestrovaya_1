using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Semestrovaya_1.Security;
using Semestrovaya_1.Templating;
using Semestrovaya_1.Models;
using Semestrovaya_1.Orm;

namespace Semestrovaya_1.Controllers;

public sealed partial class AdminController
{
    private const string CookieName = "oris_admin";

    private readonly AdminAuthService _auth;
    private readonly TemplateEngine _templates;
    private readonly string _viewsDirectory;
    private readonly OrmRepository<CardSet> _sets;
    private readonly Database _adminDatabase;

    private DateTimeOffset _nextLoginAttempt = DateTimeOffset.MinValue;
    private readonly object _loginThrottleLock = new();

    public AdminController(
        AdminAuthService auth,
        TemplateEngine templates,
        string viewsDirectory,
        Database database)
    {
        _auth = auth;
        _templates = templates;
        _viewsDirectory = viewsDirectory;
        _adminDatabase = database;
        _sets = new OrmRepository<CardSet>(database);
    }

    public async Task HandleAsync(HttpListenerContext context)
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath ?? "/";

        //заголовки безопасности
        response.Headers["Cache-Control"] = "no-store";//запрет браузеру сохранять копию
        response.Headers["X-Content-Type-Options"] = "nosniff";// запрет браузеру угадывать тип файл
        response.Headers["X-Frame-Options"] = "DENY";//запрет показывать страницу внутри iframe

        var session = _auth.FindSession(
            request.Cookies[CookieName]?.Value);

        
        
        if (path == "/admin/login")
        {
            if (request.HttpMethod == "GET")
            {
                if (session?.IsAuthenticated == true)
                {
                    Redirect(response, "/admin");
                    return;
                }

                session ??= _auth.CreateSession(false);
                SetSessionCookie(context, session);

                await ShowLoginAsync(context, session, "", 200);
                return;
            }

            if (request.HttpMethod == "POST")
            {
                var form = await ReadFormAsync(request);

                if (form is null)
                {
                    await SendAsync(
                        context, 400, "Некорректная или слишком большая форма.");
                    return;
                }

                if (!ValidCsrf(session, form))
                {
                    await SendAsync(
                        context, 403,
                        "Сессия формы истекла. Открой /admin/login заново.");
                    return;
                }

                bool throttled;

                lock (_loginThrottleLock)
                {
                    throttled = DateTimeOffset.UtcNow < _nextLoginAttempt;

                    if (!throttled)
                    {
                        _nextLoginAttempt =
                            DateTimeOffset.UtcNow.AddSeconds(2);
                    }
                }

                if (throttled)
                {
                    await ShowLoginAsync(
                        context, session!,
                        "Подожди несколько секунд перед следующей попыткой.",
                        429);
                    return;
                }

                var username = form.GetValueOrDefault("username", "").Trim();
                var password = form.GetValueOrDefault("password", "");

                if (username.Length is < 1 or > 100
                    || password.Length is < 1 or > 200
                    || !_auth.CheckCredentials(username, password))
                {
                    await ShowLoginAsync(
                        context, session!,
                        "Неверный логин или пароль.", 401);
                    return;
                }

                var authenticated = _auth.CreateSession(true);
                _auth.RemoveSession(session!.Id);

                SetSessionCookie(context, authenticated);
                Redirect(response, "/admin");
                return;
            }

            await MethodNotAllowedAsync(context, "GET, POST");
            return;
        }

        
        
        if (session?.IsAuthenticated != true)
        {
            Redirect(response, "/admin/login");
            return;
        }


        if (path == "/admin/cards"
            || path.StartsWith("/admin/cards/", StringComparison.Ordinal))
        {
            await HandleCardsAsync(context, session);
            return;
        }


        if (path == "/admin/sets"
            || path.StartsWith("/admin/sets/", StringComparison.Ordinal))
        {
            await HandleSetsAsync(context, session);
            return;
        }
       
        if (path == "/admin/domains"
            || path.StartsWith("/admin/domains/", StringComparison.Ordinal))
        {
            await HandleLookupAsync<Domain>(
                context,
                session,
                "/admin/domains",
                "Домены",
                30);

            return;
        }

        if (path == "/admin/types"
            || path.StartsWith("/admin/types/", StringComparison.Ordinal))
        {
            await HandleLookupAsync<CardType>(
                context,
                session,
                "/admin/types",
                "Типы карточек",
                30);

            return;
        }

        if (path == "/admin/tags"
            || path.StartsWith("/admin/tags/", StringComparison.Ordinal))
        {
            await HandleLookupAsync<Tag>(
                context,
                session,
                "/admin/tags",
                "Теги",
                80);

            return;
        }

        if (path == "/admin" || path == "/admin/")
        {
            if (request.HttpMethod != "GET")
            {
                await MethodNotAllowedAsync(context, "GET");
                return;
            }

            await ShowTemplateAsync(
                context,
                "index.html",
                new Dictionary<string, string>
                {
                    ["CsrfToken"] = session.CsrfToken
                },
                200);

            return;
        }

        if (path == "/admin/logout")
        {
            if (request.HttpMethod != "POST")
            {
                await MethodNotAllowedAsync(context, "POST");
                return;
            }

            var form = await ReadFormAsync(request);

            if (form is null || !ValidCsrf(session, form))
            {
                await SendAsync(context, 403, "Некорректный токен формы.");
                return;
            }

            _auth.RemoveSession(session.Id);

            response.Headers.Add(
                "Set-Cookie",
                $"{CookieName}=; Path=/admin; Max-Age=0; "
                + "HttpOnly; SameSite=Strict"
                + (request.IsSecureConnection ? "; Secure" : ""));

            Redirect(response, "/admin/login");
            return;
        }

        await SendAsync(
            context,
            404,
            "<h1>Раздел не найден</h1><a href=\"/admin\">В админку</a>");
    }

    private Task ShowLoginAsync(
        HttpListenerContext context,
        AdminSession session,
        string error,
        int status)
    {
        return ShowTemplateAsync(
            context,
            "login.html",
            new Dictionary<string, string>
            {
                ["Error"] = error,
                ["CsrfToken"] = session.CsrfToken
            },
            status);
    }

    private async Task ShowTemplateAsync(
        HttpListenerContext context,
        string fileName,
        Dictionary<string, string> values,
        int status)
    {
        var path = Path.Combine(
            _viewsDirectory, "Admin", fileName);

        var template = await File.ReadAllTextAsync(path);
        var html = _templates.Render(template, values);

        await SendAsync(context, status, html);
    }

    private static bool ValidCsrf(
        AdminSession? session,
        Dictionary<string, string> form)
    {
        return session is not null
            && form.TryGetValue("csrf", out var token)
            && string.Equals(
                token, session.CsrfToken, StringComparison.Ordinal);
    }

    private static void SetSessionCookie(
        HttpListenerContext context,
        AdminSession session)
    {
        context.Response.Headers.Add(
            "Set-Cookie",
            $"{CookieName}={session.Id}; Path=/admin; "
            + "HttpOnly; SameSite=Strict"
            + (context.Request.IsSecureConnection ? "; Secure" : ""));
    }

    private static void Redirect(
        HttpListenerResponse response,
        string location)
    {
        response.StatusCode = 303;
        response.RedirectLocation = location;
        response.ContentLength64 = 0;
    }

    private static async Task MethodNotAllowedAsync(
        HttpListenerContext context,
        string allowed)
    {
        context.Response.Headers["Allow"] = allowed;

        await SendAsync(context, 405, "Метод не поддерживается.");
    }

    private static async Task SendAsync(
        HttpListenerContext context,
        int status,
        string html)
    {
        var body = Encoding.UTF8.GetBytes(html);

        context.Response.StatusCode = status;
        context.Response.ContentType = "text/html; charset=utf-8";
        context.Response.ContentLength64 = body.Length;

        await context.Response.OutputStream.WriteAsync(body);
    }

    private static async Task<Dictionary<string, string>?> ReadFormAsync(
        HttpListenerRequest request)
    {
        const int maxBytes = 16 * 1024;

        var mediaType = request.ContentType?
            .Split(';')[0]
            .Trim();

        if (!string.Equals(
                mediaType,
                "application/x-www-form-urlencoded",
                StringComparison.OrdinalIgnoreCase)
            || request.ContentLength64 > maxBytes)
        {
            return null;
        }

        using var buffer = new MemoryStream();
        var chunk = new byte[4096];

        while (true)
        {
            var read = await request.InputStream.ReadAsync(
                chunk, 0, chunk.Length);

            if (read == 0)
            {
                break;
            }

            if (buffer.Length + read > maxBytes)
            {
                return null;
            }

            buffer.Write(chunk, 0, read);
        }

        var text = Encoding.UTF8.GetString(buffer.ToArray());
        var form = new Dictionary<string, string>(
            StringComparer.Ordinal);

        foreach (var part in text.Split(
                     '&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);

            var name = WebUtility.UrlDecode(pair[0]);
            var value = pair.Length == 2
                ? WebUtility.UrlDecode(pair[1])
                : "";

            if (!form.TryAdd(name, value))
            {
                return null;
            }
        }

        return form;
    }
}