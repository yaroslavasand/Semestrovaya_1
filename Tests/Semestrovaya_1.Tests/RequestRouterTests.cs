using Microsoft.VisualStudio.TestTools.UnitTesting;
using Semestrovaya_1.Server;

namespace Semestrovaya_1.Tests;

[TestClass]
public sealed class RequestRouterTests
{
    [TestMethod]
    [DataRow("GET")]
    [DataRow("HEAD")]
    public void Root_UsesGalleryRoute(string method)
    {
        var result = RequestRouter.Resolve(method, "/");

        Assert.AreEqual(RouteKind.Gallery, result);
    }

    [TestMethod]
    [DataRow("GET")]
    [DataRow("HEAD")]
    public void Seraphine_UsesDetailsRoute(string method)
    {
        var result = RequestRouter.Resolve(
            method,
            "/cards/seraphine");

        Assert.AreEqual(RouteKind.Seraphine, result);
    }

    [TestMethod]
    [DataRow("/Static/css/site.css")]
    [DataRow("/Static/js/gallery.js")]
    [DataRow("/Static/images/logos/riftbound.svg")]
    [DataRow("/Static/fonts/riot-inter-0.woff2")]
    public void StaticPaths_UseStaticFileHandler(string path)
    {
        var result = RequestRouter.Resolve("GET", path);

        Assert.AreEqual(RouteKind.StaticFile, result);
    }

    [TestMethod]
    public void HeadForStaticPath_UsesStaticFileHandler()
    {
        var result = RequestRouter.Resolve(
            "HEAD",
            "/Static/css/site.css");

        Assert.AreEqual(RouteKind.StaticFile, result);
    }

    [TestMethod]
    [DataRow("POST", "/")]
    [DataRow("PUT", "/cards/seraphine")]
    [DataRow("DELETE", "/Static/css/site.css")]
    [DataRow("PATCH", "/unknown")]
    public void UnsupportedPublicMethods_AreRejected(
        string method,
        string path)
    {
        var result = RequestRouter.Resolve(method, path);

        Assert.AreEqual(RouteKind.MethodNotAllowed, result);
    }

    [TestMethod]
    [DataRow("/admin")]
    [DataRow("/admin/")]
    [DataRow("/admin/login")]
    [DataRow("/admin/cards")]
    [DataRow("/admin/cards/edit")]
    public void AdminPaths_AreDelegatedToAdminController(
        string path)
    {
        var result = RequestRouter.Resolve("GET", path);

        Assert.AreEqual(RouteKind.Admin, result);
    }

    [TestMethod]
    [DataRow("/admin/login")]
    [DataRow("/admin/logout")]
    [DataRow("/admin/cards/create")]
    public void AdminPost_IsDelegatedToAdminController(
        string path)
    {
        var result = RequestRouter.Resolve("POST", path);

        Assert.AreEqual(RouteKind.Admin, result);
    }

    [TestMethod]
    [DataRow("/unknown")]
    [DataRow("/cards/unknown")]
    [DataRow("/administrator")]
    [DataRow("/admin-other")]
    [DataRow("/StaticFake/site.css")]
    [DataRow("/static/css/site.css")]
    public void UnknownPaths_AreNotFound(string path)
    {
        var result = RequestRouter.Resolve("GET", path);

        Assert.AreEqual(RouteKind.NotFound, result);
    }
}