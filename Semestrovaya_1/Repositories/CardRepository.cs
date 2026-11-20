using Npgsql;
using NpgsqlTypes;
using Semestrovaya_1.Models;
using Semestrovaya_1.Orm;

namespace Semestrovaya_1.Repositories;
//SQL-запросы для галереи
public sealed class CardRepository
{
    private readonly Database _database;

    public CardRepository(Database database)
    {
        _database = database;
    }

    public Task<List<Card>> GetAllAsync(
        string? search = null,
        string? sort = null,
        string? set = null,
        bool newOnly = false)
    {
        var searchText = (search ?? "").Trim();

        var escapedText = searchText
            .Replace("!", "!!")
            .Replace("%", "!%")
            .Replace("_", "!_");

        var pattern = "%" + escapedText + "%";

        var setCode = set switch
        {
            "all" => "",
            "radiance" => "rad",
            "vendetta" => "vendetta",
            "unleashed" => "unleashed",
            "spiritforged" => "spiritforged",
            "origins-main" => "origins-main",
            "origins-proving-grounds" => "origins-proving-grounds",
            _ => "rad"
        };

        const string rarityRank = """
                                  CASE rarity
                                      WHEN 'Common' THEN 1
                                      WHEN 'Uncommon' THEN 2
                                      WHEN 'Rare' THEN 3
                                      WHEN 'Epic' THEN 4
                                      WHEN 'Showcase' THEN 5
                                      ELSE NULL
                                  END
                                  """;

        const string cardGroup = """
                                 CASE
                                     WHEN code ~ '^[^-]+-[0-9]+[a-z]*-[0-9]+$'
                                         THEN 1
                                     WHEN code ~ '^[^-]+-r[0-9]+[a-z]*$'
                                         THEN 2
                                     WHEN code ~ '^[^-]+-sp[0-9]+[a-z]*-[0-9]+$'
                                         THEN 3
                                     ELSE NULL
                                 END
                                 """;

        const string cardNumber = """
                                  CAST(
                                      substring(
                                          code FROM '^[^-]+-(?:sp|r)?([0-9]+)'
                                      )
                                      AS numeric
                                  )
                                  """;
        var orderBy = sort switch
        {
            "card-asc" =>
                $"""
                 split_part(code, '-', 1) COLLATE "C" ASC,
                 {cardGroup} ASC NULLS LAST,
                 {cardNumber} ASC NULLS LAST,
                 code COLLATE "C" ASC
                 """,

            "card-desc" =>
                $"""
                 split_part(code, '-', 1) COLLATE "C" DESC,
                 {cardGroup} DESC NULLS LAST,
                 {cardNumber} DESC NULLS LAST,
                 code COLLATE "C" DESC
                 """,

            "name-asc" =>
                "name ASC, code ASC",

            "name-desc" =>
                "name DESC, code ASC",

            "might-asc" =>
                "might ASC NULLS LAST, code ASC",

            "might-desc" =>
                "might DESC NULLS LAST, code ASC",

            "rarity-asc" =>
                $"{rarityRank} ASC NULLS LAST, code ASC",

            "rarity-desc" =>
                $"{rarityRank} DESC NULLS LAST, code ASC",

            _ =>
                "display_order ASC, code ASC"
        };

        var sql = $"""
                   SELECT id, code, name, image_path
                   FROM public.cards
                   WHERE name ILIKE $1 ESCAPE '!'
                     AND (
                         $2 = ''
                         OR EXISTS (
                             SELECT 1
                             FROM public.card_sets AS s
                             WHERE s.id = cards.card_set_id
                               AND s.code = $2
                         )
                     )
                     AND (NOT $3 OR is_new)
                   ORDER BY {orderBy};
                   """;

        var searchParameter = new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlDbType.Text,
            Value = pattern
        };

        var setParameter = new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlDbType.Text,
            Value = setCode
        };
        var newOnlyParameter = new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlDbType.Boolean,
            Value = newOnly
        };

        return _database.QueryAsync(
            sql,
            reader => new Card
            {
                Id = reader.GetInt32(0),
                Code = reader.GetString(1),
                Name = reader.GetString(2),
                ImagePath = reader.GetString(3)
            },
            searchParameter,
            setParameter,
            newOnlyParameter);
    }
        public async Task<Dictionary<string, string>?> GetDetailsAsync(
        string code)
    {
        const string sql = """
            SELECT
                c.code AS "Code",
                c.name AS "Name",
                c.image_path AS "ImagePath",
                COALESCE(c.energy::text, '—') AS "Energy",
                COALESCE(c.power::text, '—') AS "Power",
                COALESCE(c.might::text, '—') AS "Might",
                COALESCE(NULLIF(c.rarity, ''), '—') AS "Rarity",
                COALESCE(NULLIF(c.artist, ''), '—') AS "Artist",
                COALESCE(NULLIF(c.ability, ''), '—') AS "Ability",
                s.name AS "SetName",

                COALESCE((
                    SELECT string_agg(d.name, ', ' ORDER BY d.name)
                    FROM public.card_domains AS link
                    JOIN public.domains AS d
                        ON d.id = link.domain_id
                    WHERE link.card_id = c.id
                ), '—') AS "Domains",

                COALESCE((
                    SELECT string_agg(t.name, ', ' ORDER BY t.name)
                    FROM public.card_type_links AS link
                    JOIN public.card_types AS t
                        ON t.id = link.card_type_id
                    WHERE link.card_id = c.id
                ), '—') AS "Types",

                COALESCE((
                    SELECT string_agg(t.name, ', ' ORDER BY t.name)
                    FROM public.card_tags AS link
                    JOIN public.tags AS t
                        ON t.id = link.tag_id
                    WHERE link.card_id = c.id
                ), '—') AS "Tags"

            FROM public.cards AS c
            JOIN public.card_sets AS s
                ON s.id = c.card_set_id
            WHERE c.code = $1;
            """;

        var codeParameter = new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlDbType.Text,
            Value = code
        };

        var rows = await _database.QueryAsync(
            sql,
            reader =>
            {
                var values = new Dictionary<string, string>();

                for (var i = 0; i < reader.FieldCount; i++)
                {
                    values.Add(
                        reader.GetName(i),
                        reader.GetString(i));
                }

                return values;
            },
            codeParameter);

        return rows.Count == 0 ? null : rows[0];
    }
}