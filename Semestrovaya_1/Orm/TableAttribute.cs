using System;

namespace Semestrovaya_1.Orm;

[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class TableAttribute : Attribute
{
    public string Name { get; }

    public string Schema { get; }

    public TableAttribute(
        string name,
        string schema = "public")
    {
        Name = name;
        Schema = schema;
    }
}