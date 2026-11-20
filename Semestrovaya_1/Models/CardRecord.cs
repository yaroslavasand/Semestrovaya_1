using Semestrovaya_1.Orm;

namespace Semestrovaya_1.Models;
//создание карточки в админке
[Table("cards")]
public sealed class CardRecord
{
    [Column("id", IsPrimaryKey = true, IsGenerated = true)]
    public int Id { get; set; }

    [Column("code")]
    public string Code { get; set; } = "";

    [Column("name")]
    public string Name { get; set; } = "";

    [Column("card_set_id")]
    public int CardSetId { get; set; }

    [Column("image_path")]
    public string ImagePath { get; set; } = "";

    [Column("display_order")]
    public int DisplayOrder { get; set; }
}