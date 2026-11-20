using System;
using Semestrovaya_1.Orm;

namespace Semestrovaya_1.Models;

[Table("cards")]
public sealed class CardEdit
{
    [Column("id", IsPrimaryKey = true, IsGenerated = true)]
    public int Id { get; set; }

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("energy")]
    public int? Energy { get; set; }

    [Column("power")]
    public int? Power { get; set; }

    [Column("might")]
    public int? Might { get; set; }

    [Column("artist")]
    public string Artist { get; set; } = "";

    [Column("ability")]
    public string Ability { get; set; } = "";

    [Column("is_new")]
    public bool IsNew { get; set; }

    [Column("updated_at")]
    public DateTime UpdatedAt { get; set; }
}