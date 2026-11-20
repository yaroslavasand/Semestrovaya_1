using System;
using System.IO;
using System.Linq;

namespace Semestrovaya_1.Server;
//отдаёт файлы из папки Static с проверками
public static class StaticFileResolver
{
    public static bool TryResolve(
        string staticDirectory,
        string urlPath,
        out string filePath)
    {
        
        filePath = "";

        // обработчик принимает только адреса /Static/
        if (string.IsNullOrEmpty(urlPath)
            || !urlPath.StartsWith(
                "/Static/",
                StringComparison.Ordinal))
        {
            return false;
        }

        try
        {
            var relativePath = Uri.UnescapeDataString(
                urlPath["/Static/".Length..]);
            
            if (relativePath.Length == 0
                || relativePath.Contains('\\')
                || relativePath.Contains(':')
                || relativePath.Contains('%')
                || relativePath.Any(char.IsControl))
            {
                return false;
            }

            // Разбиваем относительный адреc на отдельные папки и имя файла
            var segments = relativePath.Split('/');

            var invalidChars = Path.GetInvalidFileNameChars();

            foreach (var segment in segments)
            {
                // Не принимаем пустые части
                if (segment.Length == 0
                    || segment == "."
                    || segment == "..")
                {
                    return false;
                }

                // Проверяем допустимость каждой части пути
                if (segment.IndexOfAny(invalidChars) >= 0
                    || segment.EndsWith(' ')
                    || segment.EndsWith('.'))
                {
                    return false;
                }
            }

            // Получаем полный путь
            var root = Path.GetFullPath(staticDirectory);

            var rootPrefix = Path.EndsInDirectorySeparator(root)
                ? root
                : root + Path.DirectorySeparatorChar;

            // Собираем путь 
            var relativeFilePath = Path.Combine(segments);

            var candidate = Path.GetFullPath(
                Path.Combine(root, relativeFilePath));
            
            var comparison = OperatingSystem.IsWindows()
                ? StringComparison.OrdinalIgnoreCase
                : StringComparison.Ordinal;

            if (!candidate.StartsWith(rootPrefix, comparison))
            {
                return false;
            }

            filePath = candidate;
            return true;
        }
        
        catch (Exception exception) when (
            exception is ArgumentException
            or NotSupportedException
            or IOException)
        {

            return false;
        }
    }
}