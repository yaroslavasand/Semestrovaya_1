using System.Net;
using System.Text.RegularExpressions;
using System.Text;

namespace Semestrovaya_1.Templating;
public sealed class TemplateEngine
{
    private static readonly Regex VariablePattern = new(
        @"\{\{\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\}\}",
        RegexOptions.Compiled);

    private static readonly Regex RawVariablePattern = new(
        @"\{\{\{\s*(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\}\}\}",
        RegexOptions.Compiled);

    // Единая точка рендеринга страницы
    public string RenderPage(string template, TemplateModel model)
    {
        ArgumentNullException.ThrowIfNull(model);

        var result = RenderConditions(template, model.Conditions);

        foreach (var collection in model.Collections)
        {
            result = RenderEach(
                result,
                collection.Key,
                collection.Value);
        }

        result = Render(result, model.Values);

        if (result.Contains("{{#", StringComparison.Ordinal)
            || result.Contains("{{/", StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "В шаблоне остались необработанные блоки. "
                + "Проверьте, что все условия и коллекции "
                + "переданы в модель страницы.");
        }

        return result;
    }

    public string Render(
        string template,
        IReadOnlyDictionary<string, string> values)
    {
        // Сначала «сырые» вставки {{{Name}}} — без экранирования.
        // Используются только для доверенного HTML из админки.
        var withRawValues = RawVariablePattern.Replace(
            template,
            match =>
            {
                var name = match.Groups["name"].Value;

                if (!values.TryGetValue(name, out var value))
                {
                    throw new InvalidOperationException(
                        $"Не передано значение переменной шаблона: {name}");
                }

                return value;
            });

        // Затем обычные переменные {{Name}} — с экранированием.
        return VariablePattern.Replace(
            withRawValues,
            match =>
            {
                var name = match.Groups["name"].Value;

                if (!values.TryGetValue(name, out var value))
                {
                    throw new InvalidOperationException(
                        $"Не передано значение переменной шаблона: {name}");
                }

                return WebUtility.HtmlEncode(value);
            });
    }
    public string RenderEach(
        string template,
        string collectionName,
        IReadOnlyList<IReadOnlyDictionary<string, string>> items)
    {
        var escapedName = Regex.Escape(collectionName);

        var pattern =
            @"\{\{#each\s+"
            + escapedName
            + @"\s*\}\}(?<body>.*?)\{\{/each\}\}";

        var loopPattern = new Regex(
            pattern,
            RegexOptions.Singleline,
            TimeSpan.FromSeconds(1));

        if (!loopPattern.IsMatch(template))
        {
            if (template.Contains("{{#each", StringComparison.Ordinal))
            {
                throw new InvalidOperationException(
                    $"Некорректный цикл или неизвестная коллекция: {collectionName}.");
            }

            return template;
        }

        return loopPattern.Replace(
            template,
            match =>
            {
                var body = match.Groups["body"].Value;

                if (body.Contains("{{#each", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Вложенные циклы пока не поддерживаются.");
                }

                var result = new StringBuilder();

                foreach (var item in items)
                {
                    result.Append(Render(body, item));
                }

                return result.ToString();
            });
    }
    public string RenderConditions(
        string template,
        IReadOnlyDictionary<string, bool> conditions)
    {
        var conditionPattern = new Regex(
            @"\{\{#if\s+(?<name>[A-Za-z_][A-Za-z0-9_]*)\s*\}\}"
            + @"(?<body>.*?)"
            + @"\{\{/if\}\}",
            RegexOptions.Singleline,
            TimeSpan.FromSeconds(1));

        return conditionPattern.Replace(
            template,
            match =>
            {
                var name = match.Groups["name"].Value;
                var body = match.Groups["body"].Value;

                if (body.Contains("{{#if", StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        "Вложенные условия пока не поддерживаются.");
                }

                if (!conditions.TryGetValue(name, out var condition))
                {
                    throw new InvalidOperationException(
                        $"Не передано значение условия: {name}");
                }

                const string elseMarker = "{{else}}";

                var elseIndex = body.IndexOf(
                    elseMarker,
                    StringComparison.Ordinal);

                if (elseIndex < 0)
                {
                    return condition ? body : "";
                }

                var whenTrue = body[..elseIndex];
                var whenFalse = body[(elseIndex + elseMarker.Length)..];

                if (whenFalse.Contains(elseMarker, StringComparison.Ordinal))
                {
                    throw new InvalidOperationException(
                        $"В условии {name} найдено несколько блоков else.");
                }

                return condition ? whenTrue : whenFalse;
            });
    }
}