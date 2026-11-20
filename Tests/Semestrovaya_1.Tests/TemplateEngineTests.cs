using System;
using System.Collections.Generic;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Semestrovaya_1.Templating;

namespace Semestrovaya_1.Tests;

[TestClass]
public sealed class TemplateEngineTests
{
    [TestMethod]
    public void Render_ReplacesVariable()
    {
        var engine = new TemplateEngine();

        var values = new Dictionary<string, string>
        {
            ["Name"] = "Seraphine"
        };

        var result = engine.Render(
            "<h1>{{Name}}</h1>",
            values);

        Assert.AreEqual("<h1>Seraphine</h1>", result);
    }

    [TestMethod]
    public void Render_EncodesHtmlFromData()
    {
        var engine = new TemplateEngine();

        var values = new Dictionary<string, string>
        {
            ["Name"] = "<b>Test</b>"
        };

        var result = engine.Render("{{Name}}", values);

        Assert.AreEqual("&lt;b&gt;Test&lt;/b&gt;", result);
    }

    [TestMethod]
    public void Render_LeavesPlainTextUnchanged()
    {
        var engine = new TemplateEngine();

        var result = engine.Render(
            "Card Gallery",
            new Dictionary<string, string>());

        Assert.AreEqual("Card Gallery", result);
    }

    [TestMethod]
    public void Render_ThrowsWhenVariableIsMissing()
    {
        var engine = new TemplateEngine();

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            engine.Render(
                "{{Missing}}",
                new Dictionary<string, string>());
        });
    }

    [TestMethod]
    public void RenderEach_RendersEveryItemInOrder()
    {
        var engine = new TemplateEngine();

        var items = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                ["Name"] = "Ahri"
            },
            new Dictionary<string, string>
            {
                ["Name"] = "Seraphine"
            }
        };

        var result = engine.RenderEach(
            "{{#each Cards}}<p>{{Name}}</p>{{/each}}",
            "Cards",
            items);

        Assert.AreEqual(
            "<p>Ahri</p><p>Seraphine</p>",
            result);
    }

    [TestMethod]
    public void RenderEach_RemovesLoopForEmptyCollection()
    {
        var engine = new TemplateEngine();

        var result = engine.RenderEach(
            "Before{{#each Cards}}{{Name}}{{/each}}After",
            "Cards",
            new List<IReadOnlyDictionary<string, string>>());

        Assert.AreEqual("BeforeAfter", result);
    }

    [TestMethod]
    public void RenderEach_EncodesItemValues()
    {
        var engine = new TemplateEngine();

        var items = new List<IReadOnlyDictionary<string, string>>
        {
            new Dictionary<string, string>
            {
                ["Name"] = "<script>"
            }
        };

        var result = engine.RenderEach(
            "{{#each Cards}}{{Name}}{{/each}}",
            "Cards",
            items);

        Assert.AreEqual("&lt;script&gt;", result);
    }

    [TestMethod]
    public void RenderEach_ThrowsForUnknownCollection()
    {
        var engine = new TemplateEngine();

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            engine.RenderEach(
                "{{#each Unknown}}{{Name}}{{/each}}",
                "Cards",
                new List<IReadOnlyDictionary<string, string>>());
        });
    }

    [TestMethod]
    [DataRow(true, "Есть карточки")]
    [DataRow(false, "Карточек нет")]
    public void RenderConditions_SelectsCorrectBranch(
        bool hasCards,
        string expected)
    {
        var engine = new TemplateEngine();

        var result = engine.RenderConditions(
            "{{#if HasCards}}Есть карточки"
            + "{{else}}Карточек нет{{/if}}",
            new Dictionary<string, bool>
            {
                ["HasCards"] = hasCards
            });

        Assert.AreEqual(expected, result);
    }

    [TestMethod]
    public void RenderConditions_RemovesFalseBlockWithoutElse()
    {
        var engine = new TemplateEngine();

        var result = engine.RenderConditions(
            "Before{{#if Show}}Hidden{{/if}}After",
            new Dictionary<string, bool>
            {
                ["Show"] = false
            });

        Assert.AreEqual("BeforeAfter", result);
    }

    [TestMethod]
    public void RenderConditions_ThrowsForMissingCondition()
    {
        var engine = new TemplateEngine();

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            engine.RenderConditions(
                "{{#if Missing}}Text{{/if}}",
                new Dictionary<string, bool>());
        });
    }

    [TestMethod]
    public void RenderConditions_ThrowsForMultipleElseBlocks()
    {
        var engine = new TemplateEngine();

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            engine.RenderConditions(
                "{{#if Show}}A{{else}}B{{else}}C{{/if}}",
                new Dictionary<string, bool>
                {
                    ["Show"] = true
                });
        });
    }

    [TestMethod]
    public void GalleryPipeline_CombinesConditionAndLoop()
    {
        var engine = new TemplateEngine();

        const string template =
            "{{#if HasCards}}"
            + "{{#each Cards}}<p>{{Name}}</p>{{/each}}"
            + "{{else}}Empty{{/if}}";

        var selected = engine.RenderConditions(
            template,
            new Dictionary<string, bool>
            {
                ["HasCards"] = true
            });

        var result = engine.RenderEach(
            selected,
            "Cards",
            new List<IReadOnlyDictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    ["Name"] = "Seraphine"
                }
            });

        Assert.AreEqual("<p>Seraphine</p>", result);
    }
    
    [TestMethod]
    public void Render_RawVariableIsNotEncoded()
    {
        var engine = new TemplateEngine();

        var values = new Dictionary<string, string>
        {
            ["Ability"] = "<b>Bold</b>"
        };

        var result = engine.Render(
            "<p>{{{Ability}}}</p>",
            values);

        Assert.AreEqual("<p><b>Bold</b></p>", result);
    }

    [TestMethod]
    public void Render_RawVariableThrowsWhenMissing()
    {
        var engine = new TemplateEngine();

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            engine.Render(
                "{{{Missing}}}",
                new Dictionary<string, string>());
        });
    }
    
    [TestMethod]
    public void RenderPage_CombinesConditionLoopAndVariable()
    {
        var engine = new TemplateEngine();

        const string template =
            "<h1>{{Title}}</h1>"
            + "{{#if HasCards}}"
            + "{{#each Cards}}<p>{{Name}}</p>{{/each}}"
            + "{{else}}<p>Пусто</p>{{/if}}";

        var model = new TemplateModel();

        model.Values["Title"] = "Галерея";
        model.Conditions["HasCards"] = true;

        model.Collections["Cards"] =
            new List<IReadOnlyDictionary<string, string>>
            {
                new Dictionary<string, string>
                {
                    ["Name"] = "Seraphine"
                }
            };

        var result = engine.RenderPage(template, model);

        Assert.AreEqual(
            "<h1>Галерея</h1><p>Seraphine</p>",
            result);
    }

    [TestMethod]
    public void RenderPage_ThrowsWhenCollectionIsMissing()
    {
        var engine = new TemplateEngine();

        Assert.ThrowsException<InvalidOperationException>(() =>
        {
            engine.RenderPage(
                "{{#each Cards}}{{Name}}{{/each}}",
                new TemplateModel());
        });
    }

    [TestMethod]
    public void RenderPage_SelectsElseBranchAndSkipsLoop()
    {
        var engine = new TemplateEngine();

        const string template =
            "{{#if HasCards}}"
            + "{{#each Cards}}<p>{{Name}}</p>{{/each}}"
            + "{{else}}<p>Пусто</p>{{/if}}";

        var model = new TemplateModel();

        model.Conditions["HasCards"] = false;

        var result = engine.RenderPage(template, model);

        Assert.AreEqual("<p>Пусто</p>", result);
    }
}
