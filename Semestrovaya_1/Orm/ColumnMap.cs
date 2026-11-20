using System.Reflection;

namespace Semestrovaya_1.Orm;

public sealed class ColumnMap
{
    public PropertyInfo Property { get; }

    public string Name { get; }

    public bool IsPrimaryKey { get; }

    public bool IsGenerated { get; }

    public string SqlName { get; }

    public ColumnMap(
        PropertyInfo property,
        ColumnAttribute attribute)
    {
        Property = property;
        Name = attribute.Name;
        IsPrimaryKey = attribute.IsPrimaryKey;
        IsGenerated = attribute.IsGenerated;

        SqlName = "\"" + Name.Replace("\"", "\"\"") + "\"";
    }
}