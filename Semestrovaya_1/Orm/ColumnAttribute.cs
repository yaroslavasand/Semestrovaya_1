using System;

namespace Semestrovaya_1.Orm;

[AttributeUsage(AttributeTargets.Property, Inherited = false)]
public sealed class ColumnAttribute : Attribute
{
    public string Name { get; }

    public bool IsPrimaryKey { get; set; }

    public bool IsGenerated { get; set; }

    public ColumnAttribute(string name)
    {
        Name = name;
    }
}