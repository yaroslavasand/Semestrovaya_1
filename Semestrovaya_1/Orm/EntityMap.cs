using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Semestrovaya_1.Orm;

public sealed class EntityMap<T> where T : class, new()
{
    public string TableName { get; }

    public string Schema { get; }

    public string SqlTableName { get; }

    public IReadOnlyList<ColumnMap> Columns { get; }

    public ColumnMap PrimaryKey { get; }

    public IReadOnlyList<ColumnMap> InsertColumns { get; }

    public IReadOnlyList<ColumnMap> UpdateColumns { get; }

    public EntityMap()
    {
        var entityType = typeof(T);

        var table = entityType.GetCustomAttribute<TableAttribute>();

        if (table is null)
        {
            throw new InvalidOperationException(
                $"У модели {entityType.Name} отсутствует атрибут Table.");
        }

        if (string.IsNullOrWhiteSpace(table.Name)
            || string.IsNullOrWhiteSpace(table.Schema))
        {
            throw new InvalidOperationException(
                $"У модели {entityType.Name} не заданы таблица или схема.");
        }

        TableName = table.Name;
        Schema = table.Schema;

        SqlTableName =
            QuoteIdentifier(Schema)
            + "."
            + QuoteIdentifier(TableName);

        var columns = new List<ColumnMap>();

        var properties = entityType.GetProperties(
            BindingFlags.Public | BindingFlags.Instance);

        foreach (var property in properties)
        {
            var attribute =
                property.GetCustomAttribute<ColumnAttribute>();

            if (attribute is null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(attribute.Name))
            {
                throw new InvalidOperationException(
                    $"У свойства {entityType.Name}.{property.Name} "
                    + "не задано имя столбца.");
            }

            if (property.GetMethod?.IsPublic != true
                || property.SetMethod?.IsPublic != true
                || property.GetIndexParameters().Length != 0)
            {
                throw new InvalidOperationException(
                    $"Свойство {entityType.Name}.{property.Name} "
                    + "должно иметь публичные get и set "
                    + "и не должно быть индексатором.");
            }

            columns.Add(new ColumnMap(property, attribute));
        }

        if (columns.Count == 0)
        {
            throw new InvalidOperationException(
                $"У модели {entityType.Name} нет свойств с атрибутом Column.");
        }

        var duplicateColumn = columns
            .GroupBy(column => column.Name, StringComparer.Ordinal)
            .FirstOrDefault(group => group.Count() > 1);

        if (duplicateColumn is not null)
        {
            throw new InvalidOperationException(
                $"У модели {entityType.Name} столбец "
                + $"'{duplicateColumn.Key}' указан несколько раз.");
        }

        var primaryKeys = columns
            .Where(column => column.IsPrimaryKey)
            .ToList();

        if (primaryKeys.Count != 1)
        {
            throw new InvalidOperationException(
                $"У модели {entityType.Name} должен быть "
                + "ровно один первичный ключ.");
        }

        PrimaryKey = primaryKeys[0];

        if (PrimaryKey.Property.PropertyType != typeof(int))
        {
            throw new InvalidOperationException(
                $"Первичный ключ модели {entityType.Name} "
                + "должен иметь тип int.");
        }

        Columns = columns.AsReadOnly();

        InsertColumns = columns
            .Where(column => !column.IsGenerated)
            .ToList()
            .AsReadOnly();

        UpdateColumns = columns
            .Where(column =>
                !column.IsPrimaryKey && !column.IsGenerated)
            .ToList()
            .AsReadOnly();
    }

    private static string QuoteIdentifier(string identifier)
    {
        return "\"" + identifier.Replace("\"", "\"\"") + "\"";
    }
}