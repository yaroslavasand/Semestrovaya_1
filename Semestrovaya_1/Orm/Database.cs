using Npgsql;

namespace Semestrovaya_1.Orm;

public sealed class Database
{
    private readonly NpgsqlDataSource _dataSource;

    public Database(NpgsqlDataSource dataSource)
    {
        _dataSource = dataSource;
    }

    public async Task<List<T>> QueryAsync<T>(
        string sql,
        Func<NpgsqlDataReader, T> map,
        params NpgsqlParameter[] parameters)
    {
        await using var command = CreateCommand(sql, parameters);

        await using var reader = await command.ExecuteReaderAsync();

        var items = new List<T>();

        while (await reader.ReadAsync())
        {
            items.Add(map(reader));
        }

        return items;
    }

    public async Task<int> ExecuteAsync(
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = CreateCommand(sql, parameters);

        return await command.ExecuteNonQueryAsync();
    }

    public async Task<object?> ExecuteScalarAsync(
        string sql,
        params NpgsqlParameter[] parameters)
    {
        await using var command = CreateCommand(sql, parameters);

        var result = await command.ExecuteScalarAsync();

        return result is DBNull ? null : result;
    }
    
    public async Task<T> TransactionAsync<T>(
        Func<NpgsqlConnection, NpgsqlTransaction, Task<T>> action)
    {
        await using var connection =
            await _dataSource.OpenConnectionAsync();

        await using var transaction =
            await connection.BeginTransactionAsync();

        var result = await action(connection, transaction);

        await transaction.CommitAsync();

        return result;
    }
    private NpgsqlCommand CreateCommand(
        string sql,
        NpgsqlParameter[] parameters)
    {
        var command = _dataSource.CreateCommand(sql);

        command.Parameters.AddRange(parameters);

        return command;
    }
}