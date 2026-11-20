using Semestrovaya_1.Orm;

namespace Semestrovaya_1.Models;

[Table("domains")]
public sealed class Domain
{
    [Column("id", IsPrimaryKey = true, IsGenerated = true)]
    public int Id { get; set; }

    [Column("code")]
    public string Code { get; set; } = "";

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("description")]
    public string Description { get; set; } = "";
}