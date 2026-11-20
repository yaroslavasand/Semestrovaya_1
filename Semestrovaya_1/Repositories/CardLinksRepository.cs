using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Npgsql;
using NpgsqlTypes;
using Semestrovaya_1.Orm;

namespace Semestrovaya_1.Repositories;

public sealed record CardLinkOption(
    string Group,
    int Id,
    string Name,
    bool Selected);

public sealed class CardLinksRepository
{
    private readonly Database _database;

    public CardLinksRepository(Database database)
    {
        _database = database;
    }

    public Task<List<CardLinkOption>> GetOptionsAsync(int cardId)
    {
        const string sql = """
            SELECT
                'domains' AS group_name,
                d.id,
                d.name,
                EXISTS (
                    SELECT 1
                    FROM public.card_domains AS link
                    WHERE link.card_id = $1
                      AND link.domain_id = d.id
                ) AS selected
            FROM public.domains AS d

            UNION ALL

            SELECT
                'types',
                t.id,
                t.name,
                EXISTS (
                    SELECT 1
                    FROM public.card_type_links AS link
                    WHERE link.card_id = $1
                      AND link.card_type_id = t.id
                )
            FROM public.card_types AS t

            UNION ALL

            SELECT
                'tags',
                t.id,
                t.name,
                EXISTS (
                    SELECT 1
                    FROM public.card_tags AS link
                    WHERE link.card_id = $1
                      AND link.tag_id = t.id
                )
            FROM public.tags AS t

            ORDER BY group_name, name;
            """;

        return _database.QueryAsync(
            sql,
            reader => new CardLinkOption(
                reader.GetString(0),
                reader.GetInt32(1),
                reader.GetString(2),
                reader.GetBoolean(3)),
            new NpgsqlParameter
            {
                NpgsqlDbType = NpgsqlDbType.Integer,
                Value = cardId
            });
    }

    public Task<bool> SaveAsync(
        int cardId,
        int[] domainIds,
        int[] typeIds,
        int[] tagIds)
    {
        return _database.TransactionAsync(
            async (connection, transaction) =>
            {
                await using (var command = new NpgsqlCommand(
                    """
                    SELECT id
                    FROM public.cards
                    WHERE id = $1
                    FOR UPDATE;
                    """,
                    connection,
                    transaction))
                {
                    command.Parameters.Add(new NpgsqlParameter
                    {
                        NpgsqlDbType = NpgsqlDbType.Integer,
                        Value = cardId
                    });

                    var existing = await command.ExecuteScalarAsync();

                    if (existing is null)
                    {
                        return false;
                    }
                }

                await ReplaceLinksAsync(
                    connection,
                    transaction,
                    cardId,
                    "card_domains",
                    "domain_id",
                    domainIds);

                await ReplaceLinksAsync(
                    connection,
                    transaction,
                    cardId,
                    "card_type_links",
                    "card_type_id",
                    typeIds);

                await ReplaceLinksAsync(
                    connection,
                    transaction,
                    cardId,
                    "card_tags",
                    "tag_id",
                    tagIds);

                await using var update = new NpgsqlCommand(
                    """
                    UPDATE public.cards
                    SET updated_at = CURRENT_TIMESTAMP
                    WHERE id = $1;
                    """,
                    connection,
                    transaction);

                update.Parameters.Add(new NpgsqlParameter
                {
                    NpgsqlDbType = NpgsqlDbType.Integer,
                    Value = cardId
                });

                await update.ExecuteNonQueryAsync();

                return true;
            });
    }

    private static async Task ReplaceLinksAsync(
        NpgsqlConnection connection,
        NpgsqlTransaction transaction,
        int cardId,
        string table,
        string targetColumn,
        int[] selectedIds)
    {
        // Имена таблиц и столбцов передаются только
        // из фиксированных вызовов внутри этого класса.
        await using (var delete = new NpgsqlCommand(
            $"DELETE FROM public.{table} WHERE card_id = $1;",
            connection,
            transaction))
        {
            delete.Parameters.Add(new NpgsqlParameter
            {
                NpgsqlDbType = NpgsqlDbType.Integer,
                Value = cardId
            });

            await delete.ExecuteNonQueryAsync();
        }

        var ids = selectedIds.Distinct().ToArray();

        if (ids.Length == 0)
        {
            return;
        }

        await using var insert = new NpgsqlCommand(
            $"""
            INSERT INTO public.{table} (card_id, {targetColumn})
            SELECT $1, selected_id
            FROM unnest($2::integer[]) AS selection(selected_id);
            """,
            connection,
            transaction);

        insert.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType = NpgsqlDbType.Integer,
            Value = cardId
        });

        insert.Parameters.Add(new NpgsqlParameter
        {
            NpgsqlDbType =
                NpgsqlDbType.Array | NpgsqlDbType.Integer,
            Value = ids
        });

        await insert.ExecuteNonQueryAsync();
    }
}