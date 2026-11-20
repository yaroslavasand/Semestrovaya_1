namespace Semestrovaya_1.Models;

public sealed class Card
{
    public int Id { get; set; }

    public string Code { get; set; } = "";

    public string Name { get; set; } = "";

    public string ImagePath { get; set; } = "";
}