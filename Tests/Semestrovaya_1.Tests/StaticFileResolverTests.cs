using System.IO;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Semestrovaya_1.Server;

namespace Semestrovaya_1.Tests;

[TestClass]
public sealed class StaticFileResolverTests
{
    private static string StaticRoot =>
        Path.Combine(
            Path.GetTempPath(),
            "OrisStaticResolverTests",
            "Static");

    [TestMethod]
    [DataRow("/Static/css/site.css", "css/site.css")]
    [DataRow("/Static/js/gallery.js", "js/gallery.js")]
    [DataRow(
        "/Static/images/cards/rad-sp5-005.webp",
        "images/cards/rad-sp5-005.webp")]
    [DataRow(
        "/Static/fonts/riot-inter-0.woff2",
        "fonts/riot-inter-0.woff2")]
    public void ValidPath_ResolvesInsideStaticDirectory(
        string urlPath,
        string relativePath)
    {
  
        var expected = Path.GetFullPath(
            Path.Combine(
                StaticRoot,
                relativePath.Replace(
                    '/',
                    Path.DirectorySeparatorChar)));

  
        var success = StaticFileResolver.TryResolve(
            StaticRoot,
            urlPath,
            out var actual);

    
        Assert.IsTrue(success);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    public void EncodedSpace_IsDecoded()
    {
        var success = StaticFileResolver.TryResolve(
            StaticRoot,
            "/Static/images/test%20image.png",
            out var actual);

        var expected = Path.GetFullPath(
            Path.Combine(
                StaticRoot,
                "images",
                "test image.png"));

        Assert.IsTrue(success);
        Assert.AreEqual(expected, actual);
    }

    [TestMethod]
    [DataRow("/Static/../Program.cs")]
    [DataRow("/Static/css/../../Program.cs")]
    [DataRow("/Static/%2e%2e/Program.cs")]
    [DataRow("/Static/css/%2e%2e/site.css")]
    [DataRow("/Static/./css/site.css")]
    [DataRow("/Static/css/../site.css")]
    public void DotSegments_AreRejected(string urlPath)
    {
        var success = StaticFileResolver.TryResolve(
            StaticRoot,
            urlPath,
            out var actual);

        Assert.IsFalse(success);
        Assert.AreEqual("", actual);
    }

    [TestMethod]
    [DataRow("/Static/..\\Program.cs")]
    [DataRow("/Static/%5c..%5cProgram.cs")]
    [DataRow("/Static/C:/Windows/test.txt")]
    [DataRow("/Static/css/site.css%00")]
    [DataRow("/Static/%252e%252e/Program.cs")]
    public void DangerousCharacters_AreRejected(string urlPath)
    {
        var success = StaticFileResolver.TryResolve(
            StaticRoot,
            urlPath,
            out var actual);

        Assert.IsFalse(success);
        Assert.AreEqual("", actual);
    }

    [TestMethod]
    [DataRow("/")]
    [DataRow("/admin")]
    [DataRow("/StaticFake/css/site.css")]
    [DataRow("/static/css/site.css")]
    [DataRow("/Static/")]
    [DataRow("/Static//css/site.css")]
    [DataRow("/Static/css/")]
    public void WrongOrIncompletePaths_AreRejected(string urlPath)
    {
        var success = StaticFileResolver.TryResolve(
            StaticRoot,
            urlPath,
            out var actual);

        Assert.IsFalse(success);
        Assert.AreEqual("", actual);
    }

    [TestMethod]
    public void MissingFile_DoesNotPreventPathResolution()
    {

        var success = StaticFileResolver.TryResolve(
            StaticRoot,
            "/Static/missing.css",
            out var actual);

        var expected = Path.GetFullPath(
            Path.Combine(StaticRoot, "missing.css"));

        Assert.IsTrue(success);
        Assert.AreEqual(expected, actual);
    }
}