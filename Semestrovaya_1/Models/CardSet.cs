using Semestrovaya_1.Orm;

namespace Semestrovaya_1.Models;

[Table("card_sets")]
public sealed class CardSet
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