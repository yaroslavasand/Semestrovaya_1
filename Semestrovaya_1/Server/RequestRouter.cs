using System;

namespace Semestrovaya_1.Server;

public enum RouteKind
{
    Gallery,
    Seraphine,
    StaticFile,
    Admin,
    NotFound,
    MethodNotAllowed
}

//маршрутизатор
public static class RequestRouter
{
    public static RouteKind Resolve(
        string method,
        string path)
    {
        if (path == "/admin"
            || path.StartsWith(
                "/admin/",
                StringComparison.Ordinal))
        {
            return RouteKind.Admin;
        }

        if (method != "GET" && method != "HEAD")
        {
            return RouteKind.MethodNotAllowed;
        }

        if (path == "/")
        {
            return RouteKind.Gallery;
        }

        if (path == "/cards/seraphine")
        {
            return RouteKind.Seraphine;
        }

        if (path.StartsWith(
                "/Static/",
                StringComparison.Ordinal))
        {
            return RouteKind.StaticFile;
        }

        return RouteKind.NotFound;
    }
}