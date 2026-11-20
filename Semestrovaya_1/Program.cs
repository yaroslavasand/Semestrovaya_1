using System.Net;
using System.Text;
using Npgsql;
using Semestrovaya_1.Orm;
using Semestrovaya_1.Repositories;
using Semestrovaya_1.Templating;
using Semestrovaya_1.Controllers;
using Semestrovaya_1.Data;
using Semestrovaya_1.Security;
using Semestrovaya_1.Server;

var connectionString = Environment.GetEnvironmentVariable(
    "ORIS_DB_CONNECTION");

if (string.IsNullOrWhiteSpace(connectionString))
{
    Console.WriteLine(
        "Не задана переменная ORIS_DB_CONNECTION.");

    return;
}

// создаем пул соединений с базой
await using var dataSource =
    NpgsqlDataSource.Create(connectionString);

try
{
    await using var command = dataSource.CreateCommand(
        "SELECT current_database();");

    var databaseName = (string?)await command.ExecuteScalarAsync();

    if (databaseName != "oris_riftbound")
    {
        throw new InvalidOperationException(
            $"Подключена база '{databaseName}', "
            + "но ожидалась 'oris_riftbound'.");
    }

    Console.WriteLine($"Подключение к БД успешно: {databaseName}");
}
catch (Exception exception)
{
    Console.WriteLine(
        $"Не удалось подключиться к БД: {exception.Message}");

    return;
}

var baseDirectory = AppContext.BaseDirectory;


var database = new Database(dataSource);

if (Array.Exists(
        args,
        argument => argument == "--check-orm"))
{
    try
    {
        await OrmCrudProbe.RunAsync(database);
    }
    catch (Exception exception)
    {
        Console.WriteLine(
            $"Проверка ORM завершилась ошибкой: {exception}");

        Environment.ExitCode = 1;
    }

    return;
}

var cardRepository = new CardRepository(database);
var templateEngine = new TemplateEngine();
var adminUsername = Environment.GetEnvironmentVariable(
    "ORIS_ADMIN_USERNAME");

var adminPassword = Environment.GetEnvironmentVariable(
    "ORIS_ADMIN_PASSWORD");

if (string.IsNullOrWhiteSpace(adminUsername)
    || string.IsNullOrEmpty(adminPassword)
    || adminUsername.Length > 100
    || adminUsername != adminUsername.Trim()
    || adminPassword.Length is < 12 or > 200)
{
    Console.WriteLine(
        "Задай ORIS_ADMIN_USERNAME без пробелов по краям "
        + "(до 100 символов) и ORIS_ADMIN_PASSWORD "
        + "(от 12 до 200 символов).");

    return;
}

var adminAuth = new AdminAuthService(
    adminUsername,
    adminPassword);

var adminController = new AdminController(
    adminAuth,
    templateEngine,
    Path.Combine(baseDirectory, "Views"),
    database);

var galleryPageRenderer = new GalleryPageRenderer(
    cardRepository,
    templateEngine,
    Path.Combine(baseDirectory, "Views"));
var galleryController = new GalleryController(
    galleryPageRenderer);
var cardController = new CardController(
    cardRepository,
    templateEngine,
    Path.Combine(baseDirectory, "Views"));


var staticDirectory = Path.GetFullPath(
    Path.Combine(baseDirectory, "Static"));


var contentTypes = new Dictionary<string, string>(
    StringComparer.OrdinalIgnoreCase)
{
    [".css"] = "text/css; charset=utf-8",
    [".js"] = "text/javascript; charset=utf-8",
    [".png"] = "image/png",
    [".jpg"] = "image/jpeg",
    [".jpeg"] = "image/jpeg",
    [".webp"] = "image/webp",
    [".svg"] = "image/svg+xml",
    [".ico"] = "image/x-icon",
    [".woff"] = "font/woff",
    [".woff2"] = "font/woff2"

};

using var listener = new HttpListener();

var listenerPrefix =
    Environment.GetEnvironmentVariable("ORIS_HTTP_PREFIX")
    ?? "http://localhost:8080/";

listener.Prefixes.Add(listenerPrefix);
listener.Start();

Console.WriteLine($"Сервер запущен: {listenerPrefix}");
Console.WriteLine("Для остановки нажми Stop в Rider.");

while (true)
{
    var context = await listener.GetContextAsync();//ждем следующего запроса

    _ = HandleRequestAsync(context);//запрос пришёл, мы запускаем его обработку
                                    //и намеренно не дожидаемся её—
                                    //цикл сразу возвращается ждать следующий запрос
}

async Task HandleRequestAsync(HttpListenerContext context)
{
    try
    {
        var request = context.Request;
        var response = context.Response;
        var path = request.Url?.AbsolutePath ?? "/";

        Console.WriteLine($"{request.HttpMethod} {path}");

        var route = RequestRouter.Resolve(
            request.HttpMethod,
            path);

        if (route == RouteKind.Admin)
        {
            await adminController.HandleAsync(context);
            return;
        }

        if (route == RouteKind.MethodNotAllowed)
        {
            response.Headers["Allow"] = "GET, HEAD";

            await SendTextAsync(
                context,
                405,
                "<h1>405 — метод не поддерживается</h1>");

            return;
        }

        byte[] body;
        string contentType;

        if (route == RouteKind.Gallery)
        {
            var html = await galleryController.IndexAsync(
                request.QueryString);

            body = Encoding.UTF8.GetBytes(html);
            contentType = "text/html; charset=utf-8";
        }
        else if (route == RouteKind.Seraphine)
        {
            var html = await cardController.SeraphineAsync();

            if (html is null)
            {
                await SendTextAsync(
                    context,
                    404,
                    "<h1>404 — карточка не найдена</h1>");

                return;
            }

            body = Encoding.UTF8.GetBytes(html);
            contentType = "text/html; charset=utf-8";
        }
        else if (route == RouteKind.StaticFile)
        {
            // Проверка, что путь ведёт внутрь папки Static
            if (!StaticFileResolver.TryResolve(
                    staticDirectory,
                    path,
                    out var filePath))
            {
                await SendTextAsync(
                    context,
                    404,
                    """
                    <h1>404 — файл не найден</h1>
                    <a href="/">Вернуться на главную</a>
                    """);

                return;
            }

            // Разрешаем только расширения из нашей таблицы MIME-типов.
            var extension = Path.GetExtension(filePath);

            if (!contentTypes.TryGetValue(extension, out var fileType)
                || !File.Exists(filePath))
            {
                await SendTextAsync(
                    context,
                    404,
                    """
                    <h1>404 — файл не найден</h1>
                    <a href="/">Вернуться на главную</a>
                    """);

                return;
            }

            // после проверок читаем содержимое
            body = await File.ReadAllBytesAsync(filePath);
            contentType = fileType;
        }
        else
        {
            await SendTextAsync(
                context,
                404,
                """
                <h1>404 — страница не найдена</h1>
                <a href="/">Вернуться на главную</a>
                """);

            return;
        }

        response.StatusCode = 200;
        response.ContentType = contentType;
        response.ContentLength64 = body.Length;

        if (request.HttpMethod != "HEAD")
        {
            await response.OutputStream.WriteAsync(body);
        }
    }
    catch (Exception exception)
    {
        Console.WriteLine($"Ошибка запроса: {exception.Message}");

        try
        {
            await SendTextAsync(
                context,
                500,
                """
                <h1>500 — внутренняя ошибка сервера</h1>
                <a href="/">Вернуться на главную</a>
                """);
        }
        catch (Exception sendException)
        {
            Console.WriteLine(
                $"Не удалось отправить ответ: {sendException.Message}");
        }
    }
    finally
    {
        try
        {
            context.Response.Close();
        }
        catch (Exception closeException)
        {
            Console.WriteLine(
                $"Не удалось закрыть ответ: {closeException.Message}");
        }
    }
}

static async Task SendTextAsync(
    HttpListenerContext context,
    int statusCode,
    string html)
{
    var response = context.Response;

    var body = Encoding.UTF8.GetBytes(
        "<!DOCTYPE html><html lang=\"ru\"><head>"
        + "<meta charset=\"UTF-8\"></head><body>"
        + html
        + "</body></html>");

    response.StatusCode = statusCode;
    response.ContentType = "text/html; charset=utf-8";
    response.ContentLength64 = body.Length;

    if (context.Request.HttpMethod != "HEAD")
    {
        await response.OutputStream.WriteAsync(body);
    }
}